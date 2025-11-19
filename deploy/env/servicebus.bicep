param envFullName string = ''

param servicebusNamespaceName string = 'sbns-${envFullName}'
param servicebusqueueName string = 'sbq-${envFullName}'

param location string = resourceGroup().location

// Service Bus Namespace
resource servicebusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: servicebusNamespaceName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    premiumMessagingPartitions: 0
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: true
  }
}

// Service Bus Queue
resource servicebusQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: servicebusNamespace
  name: servicebusqueueName
  properties: {
    lockDuration: 'PT1M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: false
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    status: 'Active'
  }
}
