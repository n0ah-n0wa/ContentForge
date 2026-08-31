@description('Azure region.')
param location string

@description('Application Insights component name.')
param appInsightsName string

@description('Linked Log Analytics workspace resource ID.')
param logAnalyticsWorkspaceId string

@description('Resource tags.')
param tags object

@description('Telemetry sampling percentage (0-100). Lower in production high-traffic environments.')
param samplingPercentage int = 100

@description('When true, disables public query access (use Azure RBAC + private link for queries).')
param restrictPublicQuery bool = false

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: restrictPublicQuery ? 'Disabled' : 'Enabled'
    SamplingPercentage: samplingPercentage
  }
}

output appInsightsId string = appInsights.id
output appInsightsName string = appInsights.name

@secure()
output connectionString string = appInsights.properties.ConnectionString
