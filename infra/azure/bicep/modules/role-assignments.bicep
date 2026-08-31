@description('Principal ID of the API App Service managed identity.')
param apiManagedIdentityPrincipalId string

@description('Storage account name for media container RBAC scope.')
param storageAccountName string

@description('Media blob container name.')
param mediaContainerName string = 'media'

@description('Key Vault name for secrets RBAC scope.')
param keyVaultName string

var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b002271c9'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69d6'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storageAccount
  name: 'default'
}

resource mediaContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobService
  name: mediaContainerName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource blobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiManagedIdentityPrincipalId)) {
  name: guid(mediaContainer.id, apiManagedIdentityPrincipalId, storageBlobDataContributorRoleId)
  scope: mediaContainer
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: apiManagedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiManagedIdentityPrincipalId)) {
  name: guid(keyVault.id, apiManagedIdentityPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: apiManagedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Azure SQL database access for the managed identity is granted post-deploy (see infra/azure/README.md).

output blobRoleAssignmentId string = !empty(apiManagedIdentityPrincipalId) ? blobDataContributor.id : ''
output keyVaultRoleAssignmentId string = !empty(apiManagedIdentityPrincipalId) ? keyVaultSecretsUser.id : ''
