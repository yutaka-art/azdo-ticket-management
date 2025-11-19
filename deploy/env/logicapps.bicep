param envFullName string = ''

param logicAppsName string = 'logic-${envFullName}'
param resourcegroupName string = resourceGroup().name
param subscriptionId string = subscription().subscriptionId

param connections_teams_externalid string = '/subscriptions/${subscriptionId}/resourceGroups/${resourcegroupName}/providers/Microsoft.Web/connections/teams'

param location string = resourceGroup().location

resource logicApps_resource 'Microsoft.Logic/workflows@2019-05-01' = {
  name: logicAppsName
  location: location
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
        '$connections': {
          defaultValue: {}
          type: 'Object'
        }
      }
      triggers: {
        When_a_HTTP_request_is_received: {
          type: 'Request'
          kind: 'Http'
        }
      }
      actions: {
        'チャットやチャネルにカードを投稿する': {
          runAfter: {}
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'teams\'][\'connectionId\']'
              }
            }
            method: 'post'
            body: {
              recipient: {
                groupId: '<your-group-id>'
                channelId: '<your-channel-id>'
              }
              messageBody: '@{triggerBody()}'
            }
            path: '/v1.0/teams/conversation/adaptivecard/poster/Flow bot/location/@{encodeURIComponent(\'Channel\')}'
          }
        }
      }
      outputs: {}
    }
    parameters: {
      '$connections': {
        value: {
          teams: {
            id: '/subscriptions/${subscriptionId}/providers/Microsoft.Web/locations/japaneast/managedApis/teams'
            connectionId: connections_teams_externalid
            connectionName: 'teams'
          }
        }
      }
    }
  }
}
