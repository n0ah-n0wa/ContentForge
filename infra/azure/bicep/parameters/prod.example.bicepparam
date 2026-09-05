using '../main.bicep'

// Template only — copy to prod.bicepparam and replace every placeholder before deploying.
// CI validates that prod.bicepparam does not contain these placeholder markers.

param environment = 'prod'
param location = 'westeurope'

param azureAdAdminObjectId = '00000000-0000-0000-0000-000000000000'
param azureAdAdminLogin = 'contentforge-sql-admins@contoso.com'
param azureAdAdminPrincipalType = 'Group'

param apiContainerImage = 'contentforge.azurecr.io/contentforge-api:latest'
param webContainerImage = 'contentforge.azurecr.io/contentforge-web:latest'
param containerRegistryUrl = 'contentforge.azurecr.io'

param allowedAdminIpAddresses = []

// Required for staging/prod: acknowledges residual public SQL/storage/Key Vault endpoints
// until private endpoints are adopted (docs/operations/azure-security.md).
param acknowledgePublicDataPlaneRisks = false

param tags = {
  costCenter: 'engineering'
  owner: 'platform-team'
}
