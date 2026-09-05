using '../main.bicep'

param environment = 'prod'
param location = 'westeurope'

// Same Azure AD admin as staging until a dedicated production admin group is created.
param azureAdAdminObjectId = '43414934-5072-4fb0-8fe1-f6759fca1569'
param azureAdAdminLogin = 'dmytro.kyselov99_gmail.com#EXT#@dmytrokyselov99gmail.onmicrosoft.com'
param azureAdAdminPrincipalType = 'User'

param apiContainerImage = 'contentforgeacr.azurecr.io/contentforge-api:latest'
param webContainerImage = 'contentforgeacr.azurecr.io/contentforge-web:latest'
param containerRegistryUrl = 'contentforgeacr.azurecr.io'

// Production: no ad-hoc SQL firewall rules — use Azure AD auth and private connectivity when hardened
param allowedAdminIpAddresses = []

// Explicit acceptance of residual public data-plane endpoints (AzureCloud API allow, SQL public endpoint).
// Remains required until private endpoints / VNet integration are provisioned.
param acknowledgePublicDataPlaneRisks = true

param tags = {
  costCenter: 'engineering'
  owner: 'platform-team'
}
