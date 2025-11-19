param environmentName string
param aceName string = 'ce-${environmentName}'
param containerAppName string = 'ca-${environmentName}-azdo'
param crServerName string
param crUserName string
@secure()
param crPassword string
param crImage string
@secure()
param servicebusConnection string
param servicebusQueueName string
@secure()
param storageAccountConnection string
@secure()
param aiInstrumentationKey string
param keyVaultName string

param revisionSuffix string = ''
param isExternalIngress bool = false
param revisionMode string = 'single'
param isDaprenabled bool = true
param daprAppId string = 'dapr-backend-azdo'

param location string = resourceGroup().location

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: aceName
}

module containerApps 'apps.bicep' = {
  params: {
    containerAppName: containerAppName
    location: location
    environmentId: environment.id
    crServerName: crServerName
    crUserName: crUserName
    crPassword: crPassword
    crImage: crImage
    servicebusConnection: servicebusConnection
    aiInstrumentationKey: aiInstrumentationKey
    storageAccountConnection: storageAccountConnection
    keyVaultName: keyVaultName
    revisionSuffix: revisionSuffix
    revisionMode: revisionMode
    isExternalIngress: isExternalIngress
    isDaprenabled: isDaprenabled
    daprAppId: daprAppId
  }
}

resource daprComponentServiceBusBindings 'Microsoft.App/managedEnvironments/daprComponents@2025-01-01' = {
  parent: environment
  name: 'azdo-stream'
  properties: {
    componentType: 'bindings.azure.servicebusqueues'
    version: 'v1'
    secrets: [
      {
        name: 'connection-string'
        value: '${servicebusConnection}'
      }
    ]
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'connection-string'
      }
      {
        name: 'queueName'
        value: '${servicebusQueueName}'
      }
    ]
    scopes: [
      '${daprAppId}'
    ]
  }
  dependsOn: [
    containerApps
  ]
}

resource daprComponentCronBindings 'Microsoft.App/managedEnvironments/daprComponents@2025-01-01' = {
  parent: environment
  name: 'azdo-cron'
  properties: {
    componentType: 'bindings.cron'
    version: 'v1'
    metadata: [
      {
        name: 'schedule'
        value: '5 15 * * *'
      }
      {
        name: 'direction'
        value: 'input'
      }
    ]
    scopes: [
      '${daprAppId}'
    ]
  }
  dependsOn: [
    containerApps
  ]
}
