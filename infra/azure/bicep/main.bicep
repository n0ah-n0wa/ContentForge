// ContentForge Azure infrastructure — resource group scoped deployment.
// Provisions App Service (API + Web), Azure SQL, Blob Storage, Application Insights, and Key Vault.

targetScope = 'resourceGroup'

@description('Environment name: dev, staging, or prod.')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short project token used in resource names.')
param projectName string = 'contentforge'

@description('Azure AD administrator object ID for SQL server (group recommended).')
param azureAdAdminObjectId string

@description('Azure AD administrator login (UPN or group name) for SQL server.')
param azureAdAdminLogin string

@description('Azure AD principal type for SQL administrator: User or Group.')
@allowed(['User', 'Group'])
param azureAdAdminPrincipalType string = 'Group'

@description('API container image (full reference).')
param apiContainerImage string

@description('Web container image (full reference).')
param webContainerImage string

@description('Private container registry login server (empty when images are public).')
param containerRegistryUrl string = ''

@description('Optional IPv4 addresses for SQL firewall (developer/admin access). Omit in production.')
param allowedAdminIpAddresses array = []

@description('Common resource tags applied to every resource.')
param tags object = {}

@description('Override App Service plan SKU. When null, environment defaults apply.')
param appServicePlanSku object?

@description('Override Azure SQL database SKU. When null, environment defaults apply.')
param sqlDatabaseSku object?

@description('Application Insights sampling percentage.')
param appInsightsSamplingPercentage int?

@description('When true, blocks direct public access to the API App Service (staging/production default).')
param restrictApiPublicAccess bool?

// ---------------------------------------------------------------------------
// Naming
// ---------------------------------------------------------------------------

var env = toLower(environment)
var resourceToken = uniqueString(subscription().id, resourceGroup().id, projectName, env)

var defaultTags = union(tags, {
  environment: env
  project: projectName
  managedBy: 'bicep'
  repository: 'ContentForge'
})

var logAnalyticsName = 'log-cf-${env}'
var appInsightsName = 'appi-cf-${env}'
var storageAccountName = take('stcf${replace(env, '-', '')}${take(resourceToken, 14)}', 24)
var sqlServerName = 'sql-cf-${env}-${take(resourceToken, 6)}'
var databaseName = 'contentforge'
var keyVaultName = take('kvcf${replace(env, '-', '')}${take(resourceToken, 10)}', 24)
var appServicePlanName = 'asp-cf-${env}'
var apiAppName = 'app-cf-api-${env}'
var webAppName = 'app-cf-web-${env}'

// ---------------------------------------------------------------------------
// Environment defaults
// ---------------------------------------------------------------------------

var defaultAppServicePlanSku = env == 'prod'
  ? {
      name: 'P1v3'
      tier: 'PremiumV3'
      size: 'P1v3'
      capacity: 1
    }
  : env == 'staging'
      ? {
          name: 'B2'
          tier: 'Basic'
          size: 'B2'
          capacity: 1
        }
      : {
          name: 'B1'
          tier: 'Basic'
          size: 'B1'
          capacity: 1
        }

var defaultSqlDatabaseSku = env == 'prod'
  ? {
      name: 'S1'
      tier: 'Standard'
      capacity: 20
    }
  : env == 'staging'
      ? {
          name: 'S0'
          tier: 'Standard'
          capacity: 10
        }
      : {
          name: 'Basic'
          tier: 'Basic'
          capacity: 5
        }

var resolvedAppServicePlanSku = appServicePlanSku ?? defaultAppServicePlanSku
var resolvedSqlDatabaseSku = sqlDatabaseSku ?? defaultSqlDatabaseSku

var sqlMaxSizeBytes = env == 'prod' ? 34359738368 : 2147483648 // 32 GB prod, 2 GB non-prod
var sqlBackupRedundancy = env == 'prod' ? 'Geo' : 'Local'
var storageSku = env == 'prod' ? 'Standard_GRS' : 'Standard_LRS'
var enableBlobProtection = env == 'prod' || env == 'staging'
var logRetentionDays = env == 'prod' ? 90 : 30
var appInsightsSampling = appInsightsSamplingPercentage ?? (env == 'prod' ? 50 : 100)
var planZoneRedundant = env == 'prod' && resolvedAppServicePlanSku.tier == 'PremiumV3'
var apiAlwaysOn = env != 'dev'
var resolvedRestrictApiPublicAccess = restrictApiPublicAccess ?? (env != 'dev')
var restrictBackendPublicNetwork = env != 'dev'
var enableKeyVaultPurgeProtection = env != 'dev'
var enableSqlLongTermRetention = env == 'prod'
var sqlShortTermRetentionDays = env == 'prod' ? 35 : 7
var requireStorageInfrastructureEncryption = env == 'prod'

// Blocks direct browser access to the API URL; Web App proxy egress uses AzureCloud.
var apiInboundRestrictions = resolvedRestrictApiPublicAccess ? [
  {
    tag: 'ServiceTag'
    ipAddress: 'AzureCloud'
    action: 'Allow'
    priority: 100
    name: 'Allow Azure PaaS'
    description: 'Allow Web App proxy and Azure services; deny direct public API access'
  }
] : []

var scmInboundRestrictions = env == 'prod' ? [
  {
    tag: 'ServiceTag'
    ipAddress: 'AzureCloud'
    action: 'Allow'
    priority: 100
    name: 'Allow Azure PaaS SCM'
    description: 'Restrict Kudu/SCM to Azure PaaS sources'
  }
] : []

// ---------------------------------------------------------------------------
// Modules — foundation
// ---------------------------------------------------------------------------

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics-${env}'
  params: {
    location: location
    workspaceName: logAnalyticsName
    tags: defaultTags
    retentionInDays: logRetentionDays
  }
}

module appInsights 'modules/app-insights.bicep' = {
  name: 'app-insights-${env}'
  params: {
    location: location
    appInsightsName: appInsightsName
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: defaultTags
    samplingPercentage: appInsightsSampling
    restrictPublicQuery: env == 'prod'
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage-${env}'
  params: {
    location: location
    storageAccountName: storageAccountName
    tags: defaultTags
    skuName: storageSku
    enableBlobProtection: enableBlobProtection
    restrictPublicNetworkAccess: restrictBackendPublicNetwork
    requireInfrastructureEncryption: requireStorageInfrastructureEncryption
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql-${env}'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: databaseName
    tags: defaultTags
    azureAdAdminObjectId: azureAdAdminObjectId
    azureAdAdminLogin: azureAdAdminLogin
    azureAdAdminPrincipalType: azureAdAdminPrincipalType
    azureAdOnlyAuthentication: true
    databaseSku: resolvedSqlDatabaseSku
    maxSizeBytes: sqlMaxSizeBytes
    backupStorageRedundancy: sqlBackupRedundancy
    allowedAdminIpAddresses: allowedAdminIpAddresses
    shortTermRetentionDays: sqlShortTermRetentionDays
    enableLongTermRetention: enableSqlLongTermRetention
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault-${env}'
  params: {
    location: location
    keyVaultName: keyVaultName
    tags: defaultTags
    softDeleteRetentionInDays: env == 'prod' ? 90 : 7
    enablePurgeProtection: enableKeyVaultPurgeProtection
    restrictPublicNetworkAccess: restrictBackendPublicNetwork
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'app-service-plan-${env}'
  params: {
    location: location
    appServicePlanName: appServicePlanName
    tags: defaultTags
    sku: resolvedAppServicePlanSku
    zoneRedundant: planZoneRedundant
  }
}

// ---------------------------------------------------------------------------
// App settings (Key Vault secret must be created post-deploy; URI is stable)
// ---------------------------------------------------------------------------

var jwtSecretUri = '${keyVault.outputs.keyVaultUri}secrets/jwt-signing-key/'

var apiConnectionString = 'Server=tcp:${sql.outputs.sqlServerFqdn},1433;Initial Catalog=${sql.outputs.databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;'

var apiAppSettings = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: env == 'prod' ? 'Production' : 'Staging'
  }
  {
    name: 'Database__Provider'
    value: 'AzureSQL'
  }
  {
    name: 'Database__ConnectionString'
    value: apiConnectionString
  }
  {
    name: 'Media__Provider'
    value: 'Azure'
  }
  {
    name: 'Media__UseManagedIdentity'
    value: 'true'
  }
  {
    name: 'Media__StorageAccountName'
    value: storage.outputs.storageAccountName
  }
  {
    name: 'Media__ContainerName'
    value: 'media'
  }
  {
    name: 'Jwt__Issuer'
    value: 'ContentForge'
  }
  {
    name: 'Jwt__Audience'
    value: 'ContentForge.Admin'
  }
  {
    name: 'Media__PublicBaseUrl'
    value: '/media-files'
  }
  {
    name: 'ApplicationInsights__SamplingRatio'
    value: string(appInsightsSampling / 100)
  }
  {
    name: 'Jwt__SigningKey'
    value: '@Microsoft.KeyVault(SecretUri=${jwtSecretUri})'
  }
  {
    name: 'WEBSITES_PORT'
    value: '8080'
  }
  {
    name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
    value: 'false'
  }
]

// ---------------------------------------------------------------------------
// App Services
// ---------------------------------------------------------------------------

module apiApp 'modules/app-service.bicep' = {
  name: 'api-app-${env}'
  params: {
    location: location
    appName: apiAppName
    appServicePlanId: appServicePlan.outputs.appServicePlanId
    tags: defaultTags
    containerImage: apiContainerImage
    registryUrl: containerRegistryUrl
    appSettings: apiAppSettings
    healthCheckPath: '/health/ready'
    alwaysOn: apiAlwaysOn
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    applicationInsightsConnectionString: appInsights.outputs.connectionString
    ipSecurityRestrictionsDefaultAction: resolvedRestrictApiPublicAccess ? 'Deny' : 'Allow'
    ipSecurityRestrictions: apiInboundRestrictions
    scmIpSecurityRestrictionsDefaultAction: env == 'prod' ? 'Deny' : 'Allow'
    scmIpSecurityRestrictions: scmInboundRestrictions
    disableRemoteDebugging: true
  }
}

module webApp 'modules/app-service.bicep' = {
  name: 'web-app-${env}'
  params: {
    location: location
    appName: webAppName
    appServicePlanId: appServicePlan.outputs.appServicePlanId
    tags: defaultTags
    containerImage: webContainerImage
    registryUrl: containerRegistryUrl
    appSettings: [
      {
        name: 'WEBSITES_PORT'
        value: '8080'
      }
      {
        name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
        value: 'false'
      }
      {
        name: 'API_UPSTREAM'
        value: '${apiApp.outputs.defaultHostName}:443'
      }
      {
        name: 'API_UPSTREAM_SCHEME'
        value: 'https'
      }
      {
        name: 'API_UPSTREAM_HOST'
        value: apiApp.outputs.defaultHostName
      }
    ]
    healthCheckPath: '/'
    alwaysOn: apiAlwaysOn
    enableManagedIdentity: false
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    scmIpSecurityRestrictionsDefaultAction: env == 'prod' ? 'Deny' : 'Allow'
    scmIpSecurityRestrictions: scmInboundRestrictions
    disableRemoteDebugging: true
  }
}

module roleAssignments 'modules/role-assignments.bicep' = {
  name: 'role-assignments-${env}'
  params: {
    apiManagedIdentityPrincipalId: apiApp.outputs.principalId
    storageAccountName: storage.outputs.storageAccountName
    mediaContainerName: storage.outputs.mediaContainerName
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

output environment string = env
output resourceGroupName string = resourceGroup().name
output apiAppName string = apiApp.outputs.webAppName
output apiHostName string = apiApp.outputs.defaultHostName
output webAppName string = webApp.outputs.webAppName
output webHostName string = webApp.outputs.defaultHostName
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output databaseName string = sql.outputs.databaseName
output storageAccountName string = storage.outputs.storageAccountName
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri

@secure()
output appInsightsConnectionString string = appInsights.outputs.connectionString

output apiManagedIdentityPrincipalId string = apiApp.outputs.principalId
output restrictApiPublicAccess bool = resolvedRestrictApiPublicAccess
output postDeployNotes string = 'Create jwt-signing-key in Key Vault, grant SQL access to API MI (runtime script), run migrations (migration script), configure ACR AcrPull. See infra/azure/README.md and docs/operations/azure-security.md.'
