param envFullName string = ''

param resourcegroupName string = resourceGroup().name
param subscriptionId string = subscription().subscriptionId
param location string = resourceGroup().location

param connections_teams_name string = 'teams'

resource connections_teams_name_resource 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_teams_name
  location: location
  kind: 'V1'
  properties: {
    displayName: 'bot'
    statuses: [
      {
        status: 'Connected'
      }
    ]
    customParameterValues: {}
    nonSecretParameterValues: {}
    api: {
      name: connections_teams_name
      displayName: 'Microsoft Teams'
      description: 'Microsoft Teams では、Microsoft 365 を使用してチーム ワークスペース内のすべてのコンテンツ、ツール、会話を取得できます。'
      iconUri: 'https://conn-afd-prod-endpoint-bmc9bqahasf3grgk.b01.azurefd.net/v1.0.1757/1.0.1757.4256/${connections_teams_name}/icon.png'
      id: '/subscriptions/${subscriptionId}/providers/Microsoft.Web/locations/japaneast/managedApis/${connections_teams_name}'
      type: 'Microsoft.Web/locations/managedApis'
    }
    testLinks: [
      {
        requestUri: 'https://management.azure.com:443/subscriptions/${subscriptionId}/resourceGroups/${resourcegroupName}/providers/Microsoft.Web/connections/${connections_teams_name}/extensions/proxy/beta/me/teamwork?api-version=2016-06-01'
        method: 'get'
      }
    ]
  }
}
