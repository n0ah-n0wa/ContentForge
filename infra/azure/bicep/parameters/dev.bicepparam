using '../main.bicep'

param environment = 'dev'
param location = 'westeurope'

param azureAdAdminObjectId = '00000000-0000-0000-0000-000000000000'
param azureAdAdminLogin = 'contentforge-sql-admins@contoso.com'
param azureAdAdminPrincipalType = 'Group'

param apiContainerImage = 'contentforge.azurecr.io/contentforge-api:latest'
param webContainerImage = 'contentforge.azurecr.io/contentforge-web:latest'
param containerRegistryUrl = 'contentforge.azurecr.io'

param allowedAdminIpAddresses = []

param tags = {
  costCenter: 'engineering'
  owner: 'platform-team'
}
