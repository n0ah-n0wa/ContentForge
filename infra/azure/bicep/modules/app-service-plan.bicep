@description('Azure region.')
param location string

@description('App Service plan name.')
param appServicePlanName string

@description('Resource tags.')
param tags object

@description('Plan SKU, e.g. { name: "B1", tier: "Basic", size: "B1", capacity: 1 }.')
param sku object

@description('When true, zone redundancy is enabled (Premium v3+ only).')
param zoneRedundant bool = false

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: sku
  kind: 'linux'
  properties: {
    reserved: true
    zoneRedundant: zoneRedundant
  }
}

output appServicePlanId string = appServicePlan.id
output appServicePlanName string = appServicePlan.name
