@description('Azure region for monitoring resources.')
param location string

@description('Application Insights resource name.')
param applicationInsightsName string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

output applicationInsightsId string = appInsights.id
output applicationInsightsConnectionString string = appInsights.properties.ConnectionString
