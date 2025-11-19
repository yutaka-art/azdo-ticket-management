param envFullName string
param envShortName string
param location string = resourceGroup().location

// Log Analytics Workspace
// Application Insights
// Azure Container Apps Environment
module environment 'environment.bicep' = {
  params: {
    envFullName: envFullName
    location: location
  }
}

// Azure Container Registry
module containerRegistry 'containerregistry.bicep' = {
  params: {
    envShortName: envShortName
    location: location
  }
}

// Azure Key Vault
module keyVault 'keyvault.bicep' = {
  params: {
    envShortName: envShortName
    location: location
  }
}

// Azure Storage Account
module storageAccount 'storageaccount.bicep' = {
  params: {
    envShortName: envShortName
    location: location
  }
}

// Service Bus
module eventHubs 'servicebus.bicep' = {
  params: {
    envFullName: envFullName
    location: location
  }
}
