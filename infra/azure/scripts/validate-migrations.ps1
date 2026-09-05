# Validates EF Core migrations before deployment (Windows PowerShell equivalent of validate-migrations.sh).
# Usage (from repository root):
#   pwsh -File infra/azure/scripts/validate-migrations.ps1
#
# Requires: .NET SDK 8, dotnet-ef tool

$ErrorActionPreference = 'Stop'

$Root = Resolve-Path (Join-Path $PSScriptRoot '../../..')
Set-Location $Root

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK is required.'
}

$efOk = $false
try {
    dotnet ef --version | Out-Null
    if ($LASTEXITCODE -eq 0) { $efOk = $true }
} catch { }

if (-not $efOk) {
    Write-Host 'Installing dotnet-ef tool...'
    dotnet tool install --global dotnet-ef --version 8.0.11
    if ($LASTEXITCODE -ne 0) { throw 'Failed to install dotnet-ef' }
}

Write-Host '=== Restoring and building solution ==='
dotnet restore ContentForge.sln --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed' }
dotnet build ContentForge.sln --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

Write-Host '=== Migration safety and schema parity tests ==='
dotnet test tests/ContentForge.ArchitectureTests/ContentForge.ArchitectureTests.csproj `
  --configuration Release `
  --no-build `
  --verbosity normal `
  --filter 'FullyQualifiedName~MigrationSafetyTests|FullyQualifiedName~MigrationSchemaParityTests'
if ($LASTEXITCODE -ne 0) { throw 'Migration architecture tests failed' }

function Test-PendingModelChanges {
    param(
        [string]$Project,
        [string]$Label,
        [string]$Provider
    )

    Write-Host "=== Checking pending model changes: $Label ==="
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Database__Provider = $Provider
    dotnet ef migrations has-pending-model-changes `
      --project $Project `
      --startup-project src/ContentForge.Api `
      --context AppDbContext `
      --configuration Release `
      --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "ERROR: $Label has pending model changes or validation failed. Add and commit an EF Core migration before deploying."
    }
    Write-Host "OK: $Label model matches committed migrations."
}

Test-PendingModelChanges -Project 'src/ContentForge.Infrastructure' -Label 'PostgreSQL (ContentForge.Infrastructure)' -Provider 'PostgreSQL'
Test-PendingModelChanges -Project 'src/ContentForge.Infrastructure.SqlServer' -Label 'Azure SQL (ContentForge.Infrastructure.SqlServer)' -Provider 'AzureSQL'

Write-Host '=== Migration validation passed ==='
