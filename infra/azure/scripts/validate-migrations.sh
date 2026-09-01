#!/usr/bin/env bash
# Validates EF Core migrations before deployment.
# - Architecture tests for destructive Up() operations and snapshot parity
# - Ensures no pending model changes (PostgreSQL + Azure SQL projects)
#
# Usage (from repository root):
#   ./infra/azure/scripts/validate-migrations.sh
#
# Requires: dotnet SDK 8, dotnet-ef tool

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK is required." >&2
  exit 1
fi

if ! dotnet ef --version >/dev/null 2>&1; then
  echo "Installing dotnet-ef tool..."
  dotnet tool install --global dotnet-ef --version 8.0.11
fi

echo "=== Restoring and building solution ==="
dotnet restore ContentForge.sln --verbosity quiet
dotnet build ContentForge.sln --configuration Release --verbosity quiet

echo "=== Migration safety and schema parity tests ==="
dotnet test tests/ContentForge.ArchitectureTests/ContentForge.ArchitectureTests.csproj \
  --configuration Release \
  --no-build \
  --verbosity normal \
  --filter "FullyQualifiedName~MigrationSafetyTests|FullyQualifiedName~MigrationSchemaParityTests"

check_pending_model_changes() {
  local project="$1"
  local label="$2"
  local provider="$3"
  echo "=== Checking pending model changes: ${label} ==="
  if ASPNETCORE_ENVIRONMENT=Development Database__Provider="$provider" \
    dotnet ef migrations has-pending-model-changes \
      --project "$project" \
      --startup-project src/ContentForge.Api \
      --context AppDbContext \
      --no-build; then
    echo "OK: ${label} model matches committed migrations."
  else
    echo "ERROR: ${label} has pending model changes or validation failed." >&2
    echo "Add and commit an EF Core migration before deploying." >&2
    exit 1
  fi
}

check_pending_model_changes "src/ContentForge.Infrastructure" "PostgreSQL (ContentForge.Infrastructure)" "PostgreSQL"
check_pending_model_changes "src/ContentForge.Infrastructure.SqlServer" "Azure SQL (ContentForge.Infrastructure.SqlServer)" "AzureSQL"

echo "=== Migration validation passed ==="
