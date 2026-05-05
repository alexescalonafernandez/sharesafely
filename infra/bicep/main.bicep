// --- params ---

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

@description('Application Insights resource name.')
param applicationInsightsName string

// --- modules ---

module storage './modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    storageAccountName: storageAccountName
    blobContainerName: blobContainerName
  }
}

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    applicationInsightsName: applicationInsightsName
  }
}

// --- resources ---

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

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  tags: {
    'hidden-link: /app-insights-resource-id': appInsights.id
  }
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
    AzureStorage__AccountName: storage.outputs.storageAccountName
    AzureStorage__BlobContainerName: blobContainerName
    ShareLinks__ExpirationMinutes: string(shareLinkExpirationMinutes)
    Upload__MaxFileSizeBytes: string(maxFileSizeBytes)
    Upload__AllowedExtensions__0: allowedExtensions[0]
    Upload__AllowedExtensions__1: allowedExtensions[1]
    Upload__AllowedExtensions__2: allowedExtensions[2]
    Upload__AllowedExtensions__3: allowedExtensions[3]
    Upload__AllowedExtensions__4: allowedExtensions[4]
    Upload__LocalStoragePath: localStoragePath
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
  }
}

var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageBlobDataContributorRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions', 
  storageBlobDataContributorRoleId
)


resource storageAccountForRbac 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource webAppStorageBlobDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountForRbac.id, webApp.name, storageBlobDataContributorRoleDefinitionId)
  scope: storageAccountForRbac
  properties: {
    roleDefinitionId: storageBlobDataContributorRoleDefinitionId
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
