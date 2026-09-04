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

param tags = {
  costCenter: 'engineering'
  owner: 'platform-team'
}
