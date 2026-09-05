#!/usr/bin/env bash
# Verifies Azure SQL / storage backup-related configuration for a ContentForge environment.
# Requires: az login with access to the target subscription/resource group.
#
# Usage:
#   RESOURCE_GROUP=rg-contentforge-staging ./infra/azure/scripts/verify-backup-configuration.sh
#   RESOURCE_GROUP=rg-contentforge-prod ENVIRONMENT=prod ./infra/azure/scripts/verify-backup-configuration.sh
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:?RESOURCE_GROUP is required}"
ENVIRONMENT="${ENVIRONMENT:-}"

echo "Verifying backup configuration in resource group: $RESOURCE_GROUP"

servers="$(az sql server list -g "$RESOURCE_GROUP" --query "[].name" -o tsv)"
if [[ -z "$servers" ]]; then
  echo "ERROR: No SQL servers found in $RESOURCE_GROUP"
  exit 1
fi

while IFS= read -r server; do
  [[ -z "$server" ]] && continue
  echo "SQL server: $server"
  dbs="$(az sql db list -g "$RESOURCE_GROUP" -s "$server" --query "[?name!='master'].name" -o tsv)"
  while IFS= read -r db; do
    [[ -z "$db" ]] && continue
    echo "  Database: $db"
    short="$(az sql db short-term-retention-policy show -g "$RESOURCE_GROUP" -s "$server" -n "$db" --query retentionDays -o tsv)"
    echo "    PITR retentionDays=$short"
    if [[ "${ENVIRONMENT}" == "prod" && "$short" -lt 35 ]]; then
      echo "ERROR: Production PITR retention must be >= 35 days (found $short)"
      exit 1
    fi
    if [[ "${ENVIRONMENT}" == "prod" ]]; then
      ltr="$(az sql db ltr-policy show -g "$RESOURCE_GROUP" -s "$server" -n "$db" -o json)"
      echo "    LTR policy: $ltr"
    fi
  done <<< "$dbs"
done <<< "$servers"

accounts="$(az storage account list -g "$RESOURCE_GROUP" --query "[].name" -o tsv)"
while IFS= read -r account; do
  [[ -z "$account" ]] && continue
  echo "Storage account: $account"
  delete_retention="$(az storage account blob-service-properties show -g "$RESOURCE_GROUP" -n "$account" --query deleteRetentionPolicy.enabled -o tsv)"
  versioning="$(az storage account blob-service-properties show -g "$RESOURCE_GROUP" -n "$account" --query isVersioningEnabled -o tsv)"
  echo "    softDeleteEnabled=$delete_retention versioning=$versioning"
done <<< "$accounts"

echo "Backup configuration verification completed."
