using '../main.bicep'

param environment = 'staging'
param location = 'germanywestcentral'

param azureAdAdminObjectId = '43414934-5072-4fb0-8fe1-f6759fca1569'
param azureAdAdminLogin = 'dmytro.kyselov99_gmail.com#EXT#@dmytrokyselov99gmail.onmicrosoft.com'
param azureAdAdminPrincipalType = 'User'

param apiContainerImage = 'contentforgeacr.azurecr.io/contentforge-api:latest'
param webContainerImage = 'contentforgeacr.azurecr.io/contentforge-web:latest'
param containerRegistryUrl = 'contentforgeacr.azurecr.io'

param allowedAdminIpAddresses = []

// B2 Basic quota is often 0 on new subscriptions; B1 is sufficient for staging.
param appServicePlanSku = {
  name: 'B1'
  tier: 'Basic'
  size: 'B1'
  capacity: 1
}

param tags = {
  costCenter: 'engineering'
  owner: 'platform-team'
}
