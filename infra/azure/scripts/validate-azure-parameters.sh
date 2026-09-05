#!/usr/bin/env bash
# Validates Azure parameter files do not ship placeholder SQL admin identities for staging/prod.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
PARAMS_DIR="$ROOT/infra/azure/bicep/parameters"

fail=0

check_file() {
  local file="$1"
  local label="$2"
  if [[ ! -f "$file" ]]; then
    echo "ERROR: missing $file"
    fail=1
    return
  fi

  if grep -Eq "00000000-0000-0000-0000-000000000000" "$file"; then
    echo "ERROR: $label contains placeholder azureAdAdminObjectId"
    fail=1
  fi
  if grep -Eiq "contoso\\.com" "$file"; then
    echo "ERROR: $label contains placeholder Contoso admin login"
    fail=1
  fi
  if ! grep -Eq "acknowledgePublicDataPlaneRisks\\s*=\\s*true" "$file"; then
    echo "ERROR: $label must set acknowledgePublicDataPlaneRisks = true"
    fail=1
  fi
}

check_file "$PARAMS_DIR/staging.bicepparam" "staging.bicepparam"
check_file "$PARAMS_DIR/prod.bicepparam" "prod.bicepparam"

# Example file MUST keep placeholders (documentation template).
if [[ -f "$PARAMS_DIR/prod.example.bicepparam" ]]; then
  if ! grep -Eq "00000000-0000-0000-0000-000000000000" "$PARAMS_DIR/prod.example.bicepparam"; then
    echo "ERROR: prod.example.bicepparam should retain the empty GUID placeholder for documentation"
    fail=1
  fi
fi

if [[ "$fail" -ne 0 ]]; then
  echo "Azure parameter validation failed."
  exit 1
fi

echo "Azure parameter validation passed."
