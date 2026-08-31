@description('Azure region.')
param location string

@description('Azure SQL logical server name.')
param sqlServerName string

@description('Database name.')
param databaseName string

@description('Resource tags.')
param tags object

@description('Azure AD administrator object ID (user or group). Required for secure authentication.')
param azureAdAdminObjectId string

@description('Azure AD administrator login name (UPN or group display name).')
param azureAdAdminLogin string

@description('Azure AD principal type for the SQL administrator.')
@allowed(['User', 'Group'])
param azureAdAdminPrincipalType string = 'Group'

@description('When true, disables SQL authentication (password) entirely.')
param azureAdOnlyAuthentication bool = true

@description('Database SKU object, e.g. { name: "Basic", tier: "Basic", capacity: 5 }.')
param databaseSku object

@description('Maximum database size in bytes.')
param maxSizeBytes int

@description('Backup storage redundancy: Local, Zone, or Geo.')
param backupStorageRedundancy string = 'Local'

@description('Short-term backup retention in days (PITR window).')
param shortTermRetentionDays int = 7

@description('When true, configures long-term retention (weekly/monthly/yearly).')
param enableLongTermRetention bool = false

@description('Optional list of IPv4 addresses allowed for administrative access (dev tooling only).')
param allowedAdminIpAddresses array = []

@description('Log Analytics workspace ID for auditing and diagnostics.')
param logAnalyticsWorkspaceId string = ''

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: azureAdAdminPrincipalType
      login: azureAdAdminLogin
      sid: azureAdAdminObjectId
      tenantId: tenant().tenantId
      azureADOnlyAuthentication: azureAdOnlyAuthentication
      authenticationType: 'ActiveDirectory'
    }
  }
}

// Required for App Service managed identity connectivity without private endpoints.
// Residual exposure: mitigated by Azure AD-only authentication (no SQL passwords).
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource adminFirewallRules 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = [for (ipAddress, index) in allowedAdminIpAddresses: {
  parent: sqlServer
  name: 'AdminAccess-${index}'
  properties: {
    startIpAddress: ipAddress
    endIpAddress: ipAddress
  }
}]

resource serverAuditing 'Microsoft.Sql/servers/auditingSettings@2023-08-01-preview' = {
  parent: sqlServer
  name: 'default'
  properties: {
    state: 'Enabled'
    isAzureMonitorTargetEnabled: !empty(logAnalyticsWorkspaceId)
    retentionDays: 0
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: databaseSku
  properties: {
    maxSizeBytes: maxSizeBytes
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: backupStorageRedundancy
  }
}

resource shortTermRetention 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2023-08-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    retentionDays: shortTermRetentionDays
  }
}

resource longTermRetention 'Microsoft.Sql/servers/databases/backupLongTermRetentionPolicies@2023-08-01-preview' = if (enableLongTermRetention) {
  parent: sqlDatabase
  name: 'default'
  properties: {
    weeklyRetention: 'P4W'
    monthlyRetention: 'P12M'
    yearlyRetention: 'P5Y'
    weekOfYear: 1
  }
}

resource dbDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'send-to-log-analytics'
  scope: sqlDatabase
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'SQLSecurityAuditEvents'
        enabled: true
      }
      {
        category: 'Errors'
        enabled: true
      }
      {
        category: 'QueryStoreRuntimeStatistics'
        enabled: true
      }
      {
        category: 'QueryStoreWaitStatistics'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Basic'
        enabled: true
      }
    ]
  }
}

output sqlServerId string = sqlServer.id
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
output databaseId string = sqlDatabase.id
