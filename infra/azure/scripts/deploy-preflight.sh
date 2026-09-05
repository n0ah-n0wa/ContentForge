#!/usr/bin/env bash
# Preflight checks before ContentForge Azure deploy. Fails fast on missing config.
# Usage (from repo root, after az login):
#   GITHUB_ENVIRONMENT=staging RESOURCE_GROUP=rg-contentforge-staging \
#     API_APP=app-cf-api-staging WEB_APP=app-cf-web-staging \
#     ./infra/azure/scripts/deploy-preflight.sh
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:?RESOURCE_GROUP is required}"
API_APP="${API_APP:?API_APP is required}"
WEB_APP="${WEB_APP:?WEB_APP is required}"
GITHUB_ENVIRONMENT="${GITHUB_ENVIRONMENT:-staging}"

echo "Deploy preflight: env=$GITHUB_ENVIRONMENT rg=$RESOURCE_GROUP"

az group show -n "$RESOURCE_GROUP" >/dev/null
az webapp show -g "$RESOURCE_GROUP" -n "$API_APP" >/dev/null
az webapp show -g "$RESOURCE_GROUP" -n "$WEB_APP" >/dev/null

# JWT Key Vault reference or app setting must exist on API
jwt_setting="$(az webapp config appsettings list -g "$RESOURCE_GROUP" -n "$API_APP" --query "[?name=='Jwt__SigningKey' || name=='Jwt:SigningKey'].value" -o tsv)"
if [[ -z "$jwt_setting" ]]; then
  echo "ERROR: API app is missing Jwt__SigningKey / Jwt:SigningKey (Key Vault reference expected)."
  exit 1
fi
echo "JWT signing key app setting present."

web_host="$(az webapp show -g "$RESOURCE_GROUP" -n "$WEB_APP" --query defaultHostName -o tsv)"
echo "WEB_HOST=$web_host"

if [[ "$GITHUB_ENVIRONMENT" == "production" ]]; then
  echo "Production preflight reminder: confirm GitHub environment protection rules and staging validation flags."
fi

echo "Deploy preflight passed."
