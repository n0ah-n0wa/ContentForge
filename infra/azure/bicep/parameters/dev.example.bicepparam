using '../main.bicep'

// Copy to dev.bicepparam and replace placeholder values before deploying.
// Do not commit environment-specific object IDs if your policy treats them as sensitive.

param environment = 'dev'
param location = 'westeurope'

param azureAdAdminObjectId = '00000000-0000-0000-0000-000000000000'
param azureAdAdminLogin = 'contentforge-sql-admins@contoso.com'
param azureAdAdminPrincipalType = 'Group'

// Replace with your registry images after building and pushing contentforge-api / contentforge-web
param apiContainerImage = 'contentforge.azurecr.io/contentforge-api:latest'
param webContainerImage = 'contentforge.azurecr.io/contentforge-web:latest'
param containerRegistryUrl = 'contentforge.azurecr.io'

// Optional: developer workstation public IP for SQL admin access (Azure AD auth still required)
param allowedAdminIpAddresses = []

param tags = {
  costCenter: 'engineering'
  owner: 'platform-team'
}
