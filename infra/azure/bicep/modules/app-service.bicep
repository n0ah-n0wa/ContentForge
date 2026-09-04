@description('Azure region.')
param location string

@description('Web App name.')
param appName string

@description('App Service plan resource ID.')
param appServicePlanId string

@description('Resource tags.')
param tags object

@description('Container image reference, e.g. myregistry.azurecr.io/contentforge-api:1.0.0')
param containerImage string

@description('Registry URL when using private ACR (leave empty for Docker Hub public images).')
param registryUrl string = ''

@description('When true, enables system-assigned managed identity.')
param enableManagedIdentity bool = true

@description('Application settings (Key Vault references supported via @Microsoft.KeyVault(...)).')
param appSettings array = []

@description('Health check path for App Service probes.')
param healthCheckPath string = '/health/ready'

@description('Minimum TLS version.')
param minTlsVersion string = '1.2'

@description('When true, always-on is enabled (required for production).')
param alwaysOn bool = true

@description('Optional Log Analytics workspace ID for diagnostic settings.')
param logAnalyticsWorkspaceId string = ''

@description('Application Insights connection string (injected as app setting when non-empty).')
param applicationInsightsConnectionString string = ''

@description('Default action for inbound IP restrictions (Allow or Deny).')
param ipSecurityRestrictionsDefaultAction string = 'Allow'

@description('Inbound IP/service tag restrictions applied before the default action.')
param ipSecurityRestrictions array = []

@description('Default action for SCM (Kudu) site access.')
param scmIpSecurityRestrictionsDefaultAction string = 'Allow'

@description('Inbound restrictions for the SCM (Kudu) site.')
param scmIpSecurityRestrictions array = []

@description('When true, disables remote debugging.')
param disableRemoteDebugging bool = true

var mergedAppSettings = concat(
  appSettings,
  !empty(applicationInsightsConnectionString) ? [
    {
      name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
      value: applicationInsightsConnectionString
    }
  ] : []
)

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  tags: tags
  kind: 'app,linux,container'
  identity: enableManagedIdentity ? {
    type: 'SystemAssigned'
  } : null
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerImage}'
      alwaysOn: alwaysOn
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: minTlsVersion
      scmMinTlsVersion: minTlsVersion
      healthCheckPath: healthCheckPath
      remoteDebuggingEnabled: !disableRemoteDebugging
      acrUseManagedIdentityCreds: !empty(registryUrl)
      acrUserManagedIdentityID: null
      appSettings: mergedAppSettings
      ipSecurityRestrictionsDefaultAction: ipSecurityRestrictionsDefaultAction
      ipSecurityRestrictions: ipSecurityRestrictions
      scmIpSecurityRestrictionsDefaultAction: scmIpSecurityRestrictionsDefaultAction
      scmIpSecurityRestrictions: scmIpSecurityRestrictions
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'send-to-log-analytics'
  scope: webApp
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output webAppId string = webApp.id
output webAppName string = webApp.name
output defaultHostName string = webApp.properties.defaultHostName
output principalId string = enableManagedIdentity ? webApp.identity.principalId : ''
output outboundIpAddresses string = webApp.properties.outboundIpAddresses
output possibleOutboundIpAddresses string = webApp.properties.possibleOutboundIpAddresses
