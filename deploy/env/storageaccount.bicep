param envShortName string

param storageAccountName string = 'st${envShortName}'

param location string = resourceGroup().location

// Storage account
var strageSku = 'Standard_LRS'
resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  kind: 'Storage'
  sku:{
    name: strageSku
  }
}
