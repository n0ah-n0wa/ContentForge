#!/usr/bin/env bash
# Applies committed EF Core migrations to Azure SQL from an authenticated Azure CLI session.
#
# Safety guarantees:
# - Never drops or recreates the database
# - Never runs `dotnet ef database drop`
# - Requires the target database to exist (provisioned via Bicep)
# - Lists pending migrations before applying
# - Supports dry-run mode (list only, no changes)
# - Requires explicit acknowledgement for production
#
# Usage:
#   ./run-azure-sql-migrations.sh <resource-group> <sql-server-name> [database-name] [--dry-run]
#
# Environment:
#   CONTENTFORGE_MIGRATION_ENVIRONMENT   staging | production | dev (optional)
#   CONTENTFORGE_CONFIRM_PRODUCTION_MIGRATION=true   required when environment=production
#
# Prerequisites:
#   - az CLI logged in (or federated credentials via azure/login in CI)
#   - Caller is Azure AD SQL admin or has db_ddladmin on the target database
#   - dotnet SDK 8 + dotnet-ef tool
#   - Run from repository root

set -euo pipefail

DRY_RUN=false
POSITIONAL=()
for arg in "$@"; do
  case "$arg" in
    --dry-run)
      DRY_RUN=true
      ;;
    *)
      POSITIONAL+=("$arg")
      ;;
  esac
done
set -- "${POSITIONAL[@]}"

RESOURCE_GROUP="${1:?resource group required}"
SQL_SERVER="${2:?sql server name required}"
DATABASE="${3:-contentforge}"
RULE_NAME="gha-migrate-${GITHUB_RUN_ID:-local}-$(date +%s)"

MIGRATION_ENV="${CONTENTFORGE_MIGRATION_ENVIRONMENT:-}"
if [ "$MIGRATION_ENV" = "production" ] || [ "$MIGRATION_ENV" = "prod" ]; then
  if [ "${CONTENTFORGE_CONFIRM_PRODUCTION_MIGRATION:-}" != "true" ]; then
    echo "ERROR: Production migrations require CONTENTFORGE_CONFIRM_PRODUCTION_MIGRATION=true" >&2
    echo "Apply migrations to staging first and confirm rollback limitations in docs/operations/database-migrations.md" >&2
    exit 1
  fi
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$ROOT"

echo "=== Pre-flight migration validation ==="
chmod +x infra/azure/scripts/validate-migrations.sh
infra/azure/scripts/validate-migrations.sh

if ! az sql db show \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "$DATABASE" \
  >/dev/null 2>&1; then
  echo "ERROR: Database '${DATABASE}' does not exist on server '${SQL_SERVER}'." >&2
  echo "Provision infrastructure with Bicep before running migrations." >&2
  echo "This script never creates, drops, or recreates production databases." >&2
  exit 1
fi

RUNNER_IP="$(curl -fsSL https://api.ipify.org)"
SQL_FQDN="${SQL_SERVER}.database.windows.net"

cleanup() {
  az sql server firewall-rule delete \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER" \
    --name "$RULE_NAME" \
    >/dev/null 2>&1 || true
}
trap cleanup EXIT

echo "Allowing SQL access from runner IP ${RUNNER_IP} (rule: ${RULE_NAME})"
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER" \
  --name "$RULE_NAME" \
  --start-ip-address "$RUNNER_IP" \
  --end-ip-address "$RUNNER_IP" \
  >/dev/null

export Database__Provider=AzureSQL
export Database__ConnectionString="Server=tcp:${SQL_FQDN},1433;Database=${DATABASE};Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Default;"

if ! dotnet ef --version >/dev/null 2>&1; then
  dotnet tool install --global dotnet-ef --version 8.0.11
fi

echo "=== Applied migrations (current) ==="
dotnet ef migrations list \
  --project src/ContentForge.Infrastructure.SqlServer \
  --startup-project src/ContentForge.Api \
  --context AppDbContext \
  --no-build 2>/dev/null || dotnet ef migrations list \
  --project src/ContentForge.Infrastructure.SqlServer \
  --startup-project src/ContentForge.Api \
  --context AppDbContext

echo "=== Pending migrations ==="
PENDING=$(dotnet ef migrations list \
  --project src/ContentForge.Infrastructure.SqlServer \
  --startup-project src/ContentForge.Api \
  --context AppDbContext \
  --no-build 2>/dev/null | grep "(Pending)" || true)

if [ -z "$PENDING" ]; then
  echo "No pending migrations. Database schema is up to date."
  exit 0
fi

echo "$PENDING"

if [ "$DRY_RUN" = "true" ]; then
  echo "Dry run complete. No migrations were applied."
  exit 0
fi

echo "=== Applying pending migrations to ${SQL_FQDN}/${DATABASE} ==="
dotnet ef database update \
  --project src/ContentForge.Infrastructure.SqlServer \
  --startup-project src/ContentForge.Api \
  --context AppDbContext

echo "=== Verifying no pending migrations remain ==="
REMAINING=$(dotnet ef migrations list \
  --project src/ContentForge.Infrastructure.SqlServer \
  --startup-project src/ContentForge.Api \
  --context AppDbContext \
  --no-build | grep "(Pending)" || true)

if [ -n "$REMAINING" ]; then
  echo "ERROR: Migrations remain pending after database update:" >&2
  echo "$REMAINING" >&2
  exit 1
fi

echo "Migrations applied successfully."
