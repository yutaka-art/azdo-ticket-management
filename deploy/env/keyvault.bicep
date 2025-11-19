param envShortName string

param keyVaultName string = 'kv${envShortName}'

param location string = resourceGroup().location

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    tenantId: tenant().tenantId
  }
}

// Key Vault Secrets
resource secretOrganizatonName 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'CspFoundation--OrganizatonName'
  properties: {
    value: '-'
  }
}

resource secretProjectName 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'CspFoundation--ProjectName'
  properties: {
    value: '-'
  }
}

resource secretPersonalAccessToken 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'CspFoundation--PersonalAccessToken'
  properties: {
    value: '-'
  }
}

resource secretAzdoBaseUrl 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'CspFoundation--AzdoBaseUrl'
  properties: {
    value: 'https://dev.azure.com'
  }
}

resource secretWiqlId 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'CspFoundation--WiqlId'
  properties: {
    value: '-'
  }
}

resource LogicAppsEndPointUrl 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'CspFoundation--LogicAppsEndPointUrl'
  properties: {
    value: '-'
  }
}
