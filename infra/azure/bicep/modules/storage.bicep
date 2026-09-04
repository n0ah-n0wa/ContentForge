@description('Azure region.')
param location string

@description('Globally unique storage account name (3-24 lowercase alphanumeric).')
param storageAccountName string

@description('Blob container for media binaries.')
param mediaContainerName string = 'media'

@description('Resource tags.')
param tags object

@description('Replication type: LRS for dev, GRS or ZRS for production durability.')
param skuName string = 'Standard_LRS'

@description('When true, enables blob soft delete and versioning for recovery.')
param enableBlobProtection bool = false

@description('When true, denies public internet access; Azure PaaS bypass remains enabled.')
param restrictPublicNetworkAccess bool = true

@description('IPv4 addresses allowed through the storage firewall (App Service outbound IPs). AzureServices bypass alone does not cover App Service blob data-plane access.')
param allowedIpAddresses array = []

@description('When true, enables double encryption at the storage service level (production).')
param requireInfrastructureEncryption bool = false

@description('Log Analytics workspace ID for audit diagnostic logs.')
param logAnalyticsWorkspaceId string = ''

var storageIpRules = [
  for ip in allowedIpAddresses: {
    action: 'Allow'
    value: ip
  }
]

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    allowCrossTenantReplication: false
    publicNetworkAccess: 'Enabled'
    networkAcls: restrictPublicNetworkAccess ? {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      ipRules: storageIpRules
    } : {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
      ipRules: []
    }
    encryption: {
      requireInfrastructureEncryption: requireInfrastructureEncryption
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: enableBlobProtection ? {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    isVersioningEnabled: true
  } : {
    deleteRetentionPolicy: {
      enabled: true
      days: 3
    }
  }
}

resource mediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: mediaContainerName
  properties: {
    publicAccess: 'None'
  }
}

// StorageRead/Write/Delete categories are only valid on blob/file/queue/table services,
// not on the parent storage account resource.
resource blobDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'send-blob-to-log-analytics'
  scope: blobService
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'StorageRead'
        enabled: true
      }
      {
        category: 'StorageWrite'
        enabled: true
      }
      {
        category: 'StorageDelete'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Transaction'
        enabled: true
      }
    ]
  }
}

resource accountMetrics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: 'send-account-metrics-to-log-analytics'
  scope: storageAccount
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    metrics: [
      {
        category: 'Transaction'
        enabled: true
      }
    ]
  }
}

output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name
output mediaContainerName string = mediaContainerName
output mediaContainerResourceId string = mediaContainer.id
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
