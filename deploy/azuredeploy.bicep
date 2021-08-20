@description('Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.')
@minLength(3)
@maxLength(16)
param basename string = 'fitonfhir'
param google_client_id string
param google_client_secret string

@description('Service prinicipal ID to give permissions for key vaults.')
param spid string

param usersKvName string = 'kv-users-${basename}'
param infraKvName string = 'kv-infra-${basename}'

var fhirWriterRoleId = '3f88fce4-5892-4214-ae73-ba5294559913'
var eventHubReceiverRoleId = 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde'

resource usersKeyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: usersKvName
  location: resourceGroup().location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        tenantId: publishDataFn.identity.tenantId
        objectId: publishDataFn.identity.principalId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        tenantId: syncEventFn.identity.tenantId
        objectId: syncEventFn.identity.principalId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        tenantId: identityFn.identity.tenantId
        objectId: identityFn.identity.principalId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        tenantId: subscription().tenantId
        objectId: spid
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
    ]
    tenantId: subscription().tenantId
    enableSoftDelete: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 30
  }
}

resource infraKeyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: infraKvName
  location: resourceGroup().location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        tenantId: publishDataFn.identity.tenantId
        objectId: publishDataFn.identity.principalId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        //TODO: this is a placeholder policy that is required for provisioning but should be removed when there is a managed identity that can be used
        tenantId: subscription().tenantId
        objectId: subscription().tenantId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
    ]
    tenantId: subscription().tenantId
    enableSoftDelete: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 30
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: replace('sa-${basename}', '-', '')
  location: resourceGroup().location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
    allowSharedKeyAccess: true
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    changeFeed: {
      enabled: false
    }
    restorePolicy: {
      enabled: false
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    isVersioningEnabled: false
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2021-04-01' = {
  parent: storageAccount
  name: 'default'
}

resource queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-04-01' = {
  parent: queueService
  name: 'publish-data'
  properties: {
    metadata: {}
  }
  dependsOn: [
    storageAccount
  ]
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2021-02-01' = {
  parent: storageAccount
  name: 'default'
}

resource tableUsersTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2021-02-01' = {
  parent: tableService
  name: 'users'
}

resource queueConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: '${infraKvName}/queue-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
  }
}

resource eventHubConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: '${infraKvName}/eventhub-connection-string'
  properties: {
    value: listkeys(iotIngestAuthorizationRule.id, iotIngestAuthorizationRule.apiVersion).primaryConnectionString
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: 'la-${basename}'
  location: resourceGroup().location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai-${basename}'
  location: resourceGroup().location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2020-10-01' = {
  name: 'app-plan-${basename}'
  location: resourceGroup().location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource identityFn 'Microsoft.Web/sites@2020-06-01' = {
  name: 'identity-${basename}'
  location: resourceGroup().location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: hostingPlan.id
    clientAffinityEnabled: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
        }
        {
          name: 'GOOGLE_OAUTH_CLIENT_ID'
          value: google_client_id
        }
        {
          name: 'GOOGLE_OAUTH_CLIENT_SECRET'
          value: google_client_secret
        }
        {
          name: 'USERS_KEY_VAULT_URI'
          value: 'https://${usersKvName}${environment().suffixes.keyvaultDns}'
        }
      ]
    }
  }

  dependsOn: [
    appInsights
    hostingPlan
    storageAccount
  ]
}

resource syncEventFn 'Microsoft.Web/sites@2020-06-01' = {
  name: 'sync-event-${basename}'
  location: resourceGroup().location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: hostingPlan.id
    clientAffinityEnabled: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
        }
      ]
    }
  }

  dependsOn: [
    appInsights
    hostingPlan
    storageAccount
  ]
}

resource publishDataFn 'Microsoft.Web/sites@2020-06-01' = {
  name: 'publish-data-${basename}'
  location: resourceGroup().location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: hostingPlan.id
    clientAffinityEnabled: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
        }
        {
          name: 'EventHubConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${reference(eventHubConnectionStringSecret.id).secretUriWithVersion})'
        }
      ]
    }
  }

  dependsOn: [
    appInsights
    hostingPlan
    storageAccount
  ]
}

resource iotEventHubNamespace 'Microsoft.EventHub/namespaces@2021-01-01-preview' = {
  name: 'en-${basename}'
  location: resourceGroup().location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 2
  }
  properties: {
    zoneRedundant: true
    isAutoInflateEnabled: true
    maximumThroughputUnits: 8
    kafkaEnabled: false
  }
}

resource iotIngestEventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-01-01-preview' = {
  parent: iotEventHubNamespace
  name: 'ingest'
  properties: {
    messageRetentionInDays: 1
    partitionCount: 4
  }
}

resource iotIngestDefaultEventHubConsumerGroup 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2021-01-01-preview' = {
  parent: iotIngestEventHub
  name: '$Default'
}

resource iotIngestAuthorizationRule 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-01-01-preview' = {
  parent: iotIngestEventHub
  name: 'FunctionSender'
  properties: {
    rights: [
      'Send'
    ]
  }
}

resource workspace 'Microsoft.HealthcareApis/workspaces@2021-06-01-preview' = {
  name: replace('hw-${basename}', '-', '')
  location: resourceGroup().location
  properties: {}
}

resource fhirservice 'Microsoft.HealthcareApis/workspaces/fhirservices@2021-06-01-preview' = {
  parent: workspace
  name: 'fs-${basename}'
  location: resourceGroup().location
  kind: 'fhir-R4'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    authenticationConfiguration: {
      authority: '${environment().authentication.loginEndpoint}${subscription().tenantId}'
      audience: 'https://${workspace.name}-fs-${basename}.fhir.azurehealthcareapis.com'
      smartProxyEnabled: false
    }
  }
}

resource iotconnector 'Microsoft.HealthcareApis/workspaces/iotconnectors@2021-06-01-preview' = {
  parent: workspace
  name: 'hi-${basename}'
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    ingestionEndpointConfiguration: {
      eventHubName: iotIngestEventHub.name
      consumerGroup: iotIngestDefaultEventHubConsumerGroup.name
      fullyQualifiedEventHubNamespace: '${iotEventHubNamespace.name}.servicebus.windows.net'
    }
    deviceMapping: {
      content: {
        templateType: 'CollectionContent'
        template: [
          {
            templateType: 'JsonPathContent'
            template: {
              typeName: 'bloodglucose'
              typeMatchExpression: '$..[?(@.point[0].value[0].fpVal)]'
              timestampExpression: '$.point[0].endTimeISO8601'
              deviceIdExpression: '$.dataSourceId'
              patientIdExpression: '$.userId'
              values: [
                {
                  valueName: 'Blood Glucose'
                  valueExpression: '$.point[0].value[0].fpVal'
                  required: true
                }
              ]
            }
          }
        ]
      }
    }
  }
}

resource iotFhirWriterRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  name: guid('${resourceGroup().id}-FhirWriter')
  scope: fhirservice
  properties: {
    roleDefinitionId: '${subscription().id}/providers/Microsoft.Authorization/roleDefinitions/${fhirWriterRoleId}'
    principalId: iotconnector.identity.principalId
  }
}

resource eventHubReceiverRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  name: guid('${resourceGroup().id}-EventHubDataReceiver')
  scope: iotIngestEventHub
  properties: {
    roleDefinitionId: '${subscription().id}/providers/Microsoft.Authorization/roleDefinitions/${eventHubReceiverRoleId}'
    principalId: iotconnector.identity.principalId
  }
}

resource iotdestination 'Microsoft.HealthcareApis/workspaces/iotconnectors/fhirdestinations@2021-06-01-preview' = {
  parent: iotconnector
  name: 'hd-${basename}'
  location: resourceGroup().location
  properties: {
    resourceIdentityResolutionType: 'Create'
    fhirServiceResourceId: fhirservice.id
    fhirMapping: {
      content: {
        templateType: 'CollectionFhir'
        template: [
          {
            templateType: 'CodeValueFhir'
            template: {
              typeName: 'bloodglucose'
              value: {
                valueName: 'Blood Glucose'
                valueType: 'Quantity'
                defaultPeriod: '5000'
                unit: 'mmol/L'
                system: 'http://loinc.org'
              }
              codes: []
            }
          }
        ]
      }
    }
  }
}

output usersKeyVaultName string = usersKeyVault.name
output infraKeyVaultName string = infraKeyVault.name

output identityAppName string = identityFn.name
output syncEventAppName string = syncEventFn.name
output publishDataAppName string = publishDataFn.name
