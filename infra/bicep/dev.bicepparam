using 'main.bicep'

param location = 'westeurope'
param environment = 'dev'
param projectName = 'sharesafely'

// -- storage --
param storageAccountName = 'stsharesafelydevwe01'
param blobContainerName = 'uploads'
param deleteUploadedBlobsAfterDays = 7

// -- application insights --
param applicationInsightsName = 'appi-sharesafely-dev-we-01'

// -- web app --
param appServicePlanName = 'asp-sharesafely-dev-we-01'
param webAppName = 'app-sharesafely-dev-we-01'
param storageProvider = 'AzureBlob'
param shareLinkExpirationMinutes = 15
param maxFileSizeBytes = 5242880
param allowedExtensions = [
  '.pdf'
  '.txt'
  '.png'
  '.jpg'
  '.jpeg'
]
param localStoragePath = 'App_Data/uploads'
