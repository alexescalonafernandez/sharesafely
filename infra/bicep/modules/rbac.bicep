@description('Storage account name used as RBAC scope.')
param storageAccountName string

@description('Web App name used to generate deterministic role assignment name.')
param webAppName string

@description('Managed Identity principal ID of the Web App.')
param webAppPrincipalId string


var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageBlobDataContributorRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions', 
  storageBlobDataContributorRoleId
)

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource webAppStorageBlobDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, webAppName, storageBlobDataContributorRoleDefinitionId)
  scope: storageAccount
  properties: {
    roleDefinitionId: storageBlobDataContributorRoleDefinitionId
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}
