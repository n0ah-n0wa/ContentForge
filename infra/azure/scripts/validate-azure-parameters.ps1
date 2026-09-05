# Validates Azure parameter files do not ship placeholder SQL admin identities for staging/prod.
# Windows PowerShell equivalent of validate-azure-parameters.sh.
# Usage (from repository root):
#   powershell -NoProfile -ExecutionPolicy Bypass -File infra/azure/scripts/validate-azure-parameters.ps1

$ErrorActionPreference = 'Stop'

$Root = Resolve-Path (Join-Path $PSScriptRoot '../../..')
$ParamsDir = Join-Path $Root 'infra/azure/bicep/parameters'
$fail = 0

function Test-ParamFile {
    param(
        [string]$File,
        [string]$Label
    )

    if (-not (Test-Path $File)) {
        Write-Host "ERROR: missing $File"
        $script:fail = 1
        return
    }

    $content = Get-Content -Raw -Path $File
    if ($content -match '00000000-0000-0000-0000-000000000000') {
        Write-Host "ERROR: $Label contains placeholder azureAdAdminObjectId"
        $script:fail = 1
    }
    if ($content -match '(?i)contoso\.com') {
        Write-Host "ERROR: $Label contains placeholder Contoso admin login"
        $script:fail = 1
    }
    if ($content -notmatch 'acknowledgePublicDataPlaneRisks\s*=\s*true') {
        Write-Host "ERROR: $Label must set acknowledgePublicDataPlaneRisks = true"
        $script:fail = 1
    }
}

Test-ParamFile (Join-Path $ParamsDir 'staging.bicepparam') 'staging.bicepparam'
Test-ParamFile (Join-Path $ParamsDir 'prod.bicepparam') 'prod.bicepparam'

$example = Join-Path $ParamsDir 'prod.example.bicepparam'
if (Test-Path $example) {
    $exampleContent = Get-Content -Raw -Path $example
    if ($exampleContent -notmatch '00000000-0000-0000-0000-000000000000') {
        Write-Host 'ERROR: prod.example.bicepparam should retain the empty GUID placeholder for documentation'
        $fail = 1
    }
}

if ($fail -ne 0) {
    Write-Host 'Azure parameter validation failed.'
    exit 1
}

Write-Host 'Azure parameter validation passed.'
