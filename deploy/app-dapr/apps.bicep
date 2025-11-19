param containerAppName string
param location string = resourceGroup().location
param environmentId string
param crServerName string
param crUserName string
@secure()
param crPassword string
param crImage string

@secure()
param servicebusConnection string
@secure()
param storageAccountConnection string

@secure()
param aiInstrumentationKey string

param keyVaultName string

param revisionSuffix string
param isExternalIngress bool
param isDaprenabled bool
param daprAppId string

@allowed([
  'multiple'
  'single'
])
param revisionMode string = 'single'
param servicebusConnectionSecretName string = 'servicebus-connection'
param blobConnectionSecretName string = 'blob-connection'
param aiConnectionSecretName string = 'ai-connection'

resource containerApp 'Microsoft.App/containerApps@2025-01-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      secrets: [
        {
          name: 'servicebus-connection'
          value: '${servicebusConnection}'
        }
        {
          name: 'blob-connection'
          value: '${storageAccountConnection}'
        }
        {
          name: 'container-registry-password'
          value: '${crPassword}'
        }
        {
          name: 'ai-connection'
          value: '${aiInstrumentationKey}'
        }
      ]
      registries: [
        {
          server: '${crServerName}'
          username: '${crUserName}'
          passwordSecretRef: 'container-registry-password'
        }
      ]
      activeRevisionsMode: revisionMode
      ingress: {
        external: isExternalIngress
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      dapr: {
        enabled: isDaprenabled
        appId: daprAppId
        appPort: 8080
        appProtocol: 'http'
      }
    }
    template: {
      revisionSuffix: revisionSuffix
      containers: [
        {
          image: crImage
          name: containerAppName
          resources: {
            cpu: '0.25'
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'TZ'
              value: 'Asia/Tokyo'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'STORAGE_CONNECT_STRING'
              secretRef: blobConnectionSecretName
            }
            {
              name: 'KEY_VAULT_URL'
              value: 'https://${keyVaultName}.vault.azure.net/'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Prod'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: aiConnectionSecretName
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling-rule'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
