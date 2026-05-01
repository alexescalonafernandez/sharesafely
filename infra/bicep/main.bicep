targetScope = 'resourceGroup'

@description('Azure region for all ShareSafely resources.')
param location string = resourceGroup().location

@description('Environment name (e.g., dev, staging, prod) for naming resources.')
param environment string = 'dev'

@description('Project name.')
param projectName string = 'sharesafely'

@description('Storage account name.')
param storageAccountName string

@description('Blob container name for uploaded files.')
param blobContainerName string = 'uploads'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource uploadsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: '${storageAccount.name}/default/${blobContainerName}'
  properties: {
    publicAccess: 'None'
    defaultEncryptionScope: '$account-encryption-key'
    denyEncryptionScopeOverride: false
  }
}
