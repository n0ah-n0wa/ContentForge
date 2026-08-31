using '../main.bicep'

param environment = 'prod'
param location = 'westeurope'

param azureAdAdminObjectId = '00000000-0000-0000-0000-000000000000'
param azureAdAdminLogin = 'contentforge-sql-admins@contoso.com'
param azureAdAdminPrincipalType = 'Group'

param apiContainerImage = 'contentforge.azurecr.io/contentforge-api:latest'
param webContainerImage = 'contentforge.azurecr.io/contentforge-web:latest'
param containerRegistryUrl = 'contentforge.azurecr.io'

// Production: no ad-hoc SQL firewall rules — use Azure AD auth and private connectivity when hardened
param allowedAdminIpAddresses = []

param tags = {
  costCenter: 'engineering'
  owner: 'platform-team'
}

// Optional overrides (uncomment to customize):
// param appServicePlanSku = {
//   name: 'P2v3'
//   tier: 'PremiumV3'
//   size: 'P2v3'
//   capacity: 2
// }
// param sqlDatabaseSku = {
//   name: 'S2'
//   tier: 'Standard'
//   capacity: 50
// }
// param appInsightsSamplingPercentage = 25
