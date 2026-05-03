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

@description('App Service Plan name.')
param appServicePlanName string

@description('Web App name.')
param webAppName string

@description('Storage provider used by the application.')
param storageProvider string = 'AzureBlob'

@description('Share link expiration time in minutes.')
param shareLinkExpirationMinutes int = 15

@description('Maximum allowed upload file size in bytes.')
param maxFileSizeBytes int = 5242880

@description('Allowed upload file extensions.')
param allowedExtensions array = [
  '.pdf'
  '.txt'
  '.png'
  '.jpg'
  '.jpeg'
]

@description('Local storage path used only when local provider is selected.')
param localStoragePath string = 'App_Data/uploads'


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

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    family: 'B'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      localMySqlEnabled: false
    }
    httpsOnly: true
  }
}

resource webAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: webApp
  name: 'appsettings'
  properties: {
    Storage__Provider: storageProvider
    AzureStorage__AccountName: storageAccount.name
    AzureStorage__BlobContainerName: blobContainerName
    ShareLinks__ExpirationMinutes: string(shareLinkExpirationMinutes)
    Upload__MaxFileSizeBytes: string(maxFileSizeBytes)
    Upload__AllowedExtensions__0: allowedExtensions[0]
    Upload__AllowedExtensions__1: allowedExtensions[1]
    Upload__AllowedExtensions__2: allowedExtensions[2]
    Upload__AllowedExtensions__3: allowedExtensions[3]
    Upload__AllowedExtensions__4: allowedExtensions[4]
    Upload__LocalStoragePath: localStoragePath
  }
}
