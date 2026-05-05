targetScope = 'resourceGroup'

@description('Azure region for all ShareSafely resources.')
param location string = resourceGroup().location

@description('Environment name (e.g., dev, staging, prod) for naming resources.')
param environment string = 'dev'

@description('Project name.')
param projectName string = 'sharesafely'


// -- storage --
@description('Storage account name.')
param storageAccountName string

@description('Blob container name for uploaded files.')
param blobContainerName string = 'uploads'

@description('Number of days after last modification before uploaded blobs are deleted.')
param deleteUploadedBlobsAfterDays int = 7


// -- application insights --
@description('Application Insights resource name.')
param applicationInsightsName string


// -- web app --
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


// --- modules ---
module storage './modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    storageAccountName: storageAccountName
    blobContainerName: blobContainerName
    deleteUploadedBlobsAfterDays: deleteUploadedBlobsAfterDays
  }
}

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    applicationInsightsName: applicationInsightsName
  }
}

module webapp './modules/webapp.bicep' = {
  name: 'webapp'
  params: {
    location: location
    appServicePlanName: appServicePlanName
    webAppName: webAppName
    storageProvider: storageProvider
    storageAccountName: storage.outputs.storageAccountName
    blobContainerName: blobContainerName
    shareLinkExpirationMinutes: shareLinkExpirationMinutes
    maxFileSizeBytes: maxFileSizeBytes
    allowedExtensions: allowedExtensions
    localStoragePath: localStoragePath
    applicationInsightsName: applicationInsightsName
  }
}

module rbac './modules/rbac.bicep' = {
  name: 'rbac'
  params: {
    storageAccountName: storageAccountName
    webAppName: webAppName
    webAppPrincipalId: webapp.outputs.webAppPrincipalId
  }
}
