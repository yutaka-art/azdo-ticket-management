param envShortName string

param acrName string = 'cr${envShortName}'

param location string = resourceGroup().location

// Azure Container Registry
param acrSku string = 'Basic'
resource acrResource 'Microsoft.ContainerRegistry/registries@2025-04-01' = {
  name: acrName
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: true
  }
}
