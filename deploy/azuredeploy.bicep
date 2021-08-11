@description('Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.')
@minLength(3)
@maxLength(16)
param basename string = 'fitonfhir'

// @description('Json string containing the device mapping')
// param deviceMapping string

var fhirWriterRoleId = '3f88fce4-5892-4214-ae73-ba5294559913'
var eventHubReceiverRoleId = 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde'

resource healthcareWorkspace 'Microsoft.HealthcareApis/workspaces@2021-06-01-preview' = {
  name: replace('hw-${basename}', '-', '')
  location: resourceGroup().location
}

resource iotConnector 'Microsoft.HealthcareApis/workspaces/iotconnectors@2021-06-01-preview' = {
  name: 'iot-connector'
  location: resourceGroup().location
  parent: healthcareWorkspace
  dependsOn: [
    healthcareWorkspace
    iotEventHubNamespace
    iotEventHub
  ]
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    ingestionEndpointConfiguration: {
      eventHubName: iotEventHub.name
      fullyQualifiedEventHubNamespace: '${iotEventHubNamespace.name}.servicebus.windows.net'
      consumerGroup: '$Default'
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

resource iotConnectorDestination 'Microsoft.HealthcareApis/workspaces/iotconnectors/destinations@2020-11-01-preview' = {
  name: 'dest1'
  location: resourceGroup().location
  parent: iotConnector
  dependsOn: [
    iotConnector
    fhirService
  ]
  properties: {
    destinationType: 'FhirServer'
    resourceIdentityResolutionType: 'Create'
    fhirServiceResourceId: fhirService.id
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

resource iotEventHubNamespace 'Microsoft.EventHub/namespaces@2021-01-01-preview' = {
  name: 'en-${basename}'
  location: resourceGroup().location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    zoneRedundant: true
  }
}

resource iotEventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-01-01-preview' = {
  name: 'ingest'
  parent: iotEventHubNamespace
  properties: {
    messageRetentionInDays: 1
    partitionCount: 2
  }
}

resource fhirService 'Microsoft.HealthcareApis/workspaces/fhirservices@2021-06-01-preview' = {
  name: 'hs-${basename}'
  location: resourceGroup().location
  dependsOn: [
    healthcareWorkspace
  ]
  parent: healthcareWorkspace
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'fhir-R4'
  properties: {
    authenticationConfiguration: {
      audience: 'https://${healthcareWorkspace.name}-ha-${basename}.azurehealthcareapis.com'
      authority: uri(environment().authentication.loginEndpoint, subscription().tenantId)
      smartProxyEnabled: false
    }
  }
}

resource iotFhirWriterRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  name: guid('${resourceGroup().id}-FhirWriter')
  scope: fhirService
  properties: {
    roleDefinitionId: '${subscription().id}/providers/Microsoft.Authorization/roleDefinitions/${fhirWriterRoleId}'
    principalId: iotConnector.identity.principalId
  }
}

resource eventHubReceiverRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  name: guid('${resourceGroup().id}-EventHubDataReceiver')
  scope: iotEventHub
  properties: {
    roleDefinitionId: '${subscription().id}/providers/Microsoft.Authorization/roleDefinitions/${eventHubReceiverRoleId}'
    principalId: iotConnector.identity.principalId
  }
}

resource usersKeyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: 'kv-users-${basename}'
  location: resourceGroup().location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
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

resource infraKeyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: 'kv-infra-${basename}'
  location: resourceGroup().location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
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
  name: 'google-fit-users'
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

resource queueSecret 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: '${infraKeyVault.name}/queue-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
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
          name: 'EventHubConnectionString'
          value: listkeys(iotEventHubNamespace.id, '2020-05-01-preview').primaryConnectionString
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
      ]
    }
  }

  dependsOn: [
    appInsights
    hostingPlan
    storageAccount
  ]
}

output usersKeyVaultName string = usersKeyVault.name
output infraKeyVaultName string = infraKeyVault.name
