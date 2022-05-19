@description('Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.')
@minLength(3)
@maxLength(16)
param basename string = 'fitonfhir'

@description('Service prinicipal ID to give permissions for key vaults.')
param location string = resourceGroup().location
param google_client_id string
@secure()
param google_client_secret string

@description('Service prinicipal ID to give permissions for key vaults.')
param spid string

@description('The repository where the googlefit-on-fhir source code resides.')
param repository_url string = 'https://github.com/Microsoft/googlefit-on-fhir'

@description('The source code branch to be deployed')
param repository_branch string = 'main'
param usersKvName string = 'kv-users-${basename}'
param infraKvName string = 'kv-infra-${basename}'

var fhirWriterRoleId = '3f88fce4-5892-4214-ae73-ba5294559913'
var eventHubReceiverRoleId = 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde'

resource usersKvName_resource 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: usersKvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        tenantId: reference(import_data_basename.id, '2020-06-01', 'Full').identity.tenantId
        objectId: reference(import_data_basename.id, '2020-06-01', 'Full').identity.principalId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        tenantId: reference(import_timer_basename.id, '2020-06-01', 'Full').identity.tenantId
        objectId: reference(import_timer_basename.id, '2020-06-01', 'Full').identity.principalId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        tenantId: reference(authorize_basename.id, '2020-06-01', 'Full').identity.tenantId
        objectId: reference(authorize_basename.id, '2020-06-01', 'Full').identity.principalId
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

resource infraKvName_resource 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: infraKvName
  location: location
  properties: {
    accessPolicies: []
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableSoftDelete: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 30
  }
}

resource infraKvName_add 'Microsoft.KeyVault/vaults/accessPolicies@2019-09-01' = {
  parent: infraKvName_resource
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: reference(import_data_basename.id, '2020-06-01', 'full').identity.tenantId
        objectId: reference(import_data_basename.id, '2020-06-01', 'full').identity.principalId
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
      {
        tenantId: subscription().tenantId
        objectId: subscription().tenantId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
    ]
  }
}

resource sa_basename 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: replace('sa-${basename}', '-', '')
  location: location
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
        file: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource sa_basename_default 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' = {
  name: '${replace('sa-${basename}', '-', '')}/default'
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
  dependsOn: [
    sa_basename
  ]
}

resource Microsoft_Storage_storageAccounts_fileServices_sa_basename_default 'Microsoft.Storage/storageAccounts/fileServices@2021-04-01' = {
  name: '${replace('sa-${basename}', '-', '')}/default'
  properties: {}
  dependsOn: [
    sa_basename
  ]
}

resource Microsoft_Storage_storageAccounts_queueServices_sa_basename_default 'Microsoft.Storage/storageAccounts/queueServices@2021-04-01' = {
  name: '${replace('sa-${basename}', '-', '')}/default'
  dependsOn: [
    sa_basename
  ]
}

resource sa_basename_default_import_data 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-04-01' = {
  parent: Microsoft_Storage_storageAccounts_queueServices_sa_basename_default
  name: 'import-data'
  properties: {
    metadata: {}
  }
  dependsOn: [
    sa_basename
  ]
}

resource Microsoft_Storage_storageAccounts_tableServices_sa_basename_default 'Microsoft.Storage/storageAccounts/tableServices@2021-02-01' = {
  name: '${replace('sa-${basename}', '-', '')}/default'
  dependsOn: [
    sa_basename
  ]
}

resource sa_basename_default_users 'Microsoft.Storage/storageAccounts/tableServices/tables@2021-02-01' = {
  parent: Microsoft_Storage_storageAccounts_tableServices_sa_basename_default
  name: 'users'
  dependsOn: [
    sa_basename
  ]
}

resource infraKvName_queue_connection_string 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: infraKvName_resource
  name: 'queue-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
  }
}

resource infraKvName_eventhub_connection_string 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: infraKvName_resource
  name: 'eventhub-connection-string'
  properties: {
    value: listkeys(en_basename_ingest_FunctionSender.id, '2021-01-01-preview').primaryConnectionString
  }
}

resource la_basename 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: 'la-${basename}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource ai_basename 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai-${basename}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: la_basename.id
  }
}

resource app_plan_basename 'Microsoft.Web/serverfarms@2020-10-01' = {
  name: 'app-plan-${basename}'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
    capacity: 0
  }
  kind: 'functionapp'
  properties: {
    perSiteScaling: false
    maximumElasticWorkerCount: 1
    isSpot: false
    reserved: false
    isXenon: false
    hyperV: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
  }
}

resource authorize_basename 'Microsoft.Web/sites@2020-06-01' = {
  name: 'authorize-${basename}'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    enabled: true
    httpsOnly: true
    serverFarmId: app_plan_basename.id
    reserved: false
    scmSiteAlsoStopped: false
    clientAffinityEnabled: true
    clientCertEnabled: false
    hostNamesDisabled: false
    containerSize: 1536
    dailyMemoryTimeQuota: 0
  }
}

resource authorize_basename_appsettings 'Microsoft.Web/sites/config@2015-08-01' = {
  parent: authorize_basename
  location: location
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    PROJECT: 'src/Authorization/FitOnFhir.Authorization/FitOnFhir.Authorization.csproj'
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
    WEBSITE_CONTENTSHARE: 'authorize-${basename}-${take(uniqueString('authorize-', basename), 4)}'
    GOOGLE_OAUTH_CLIENT_ID: google_client_id
    GOOGLE_OAUTH_CLIENT_SECRET: google_client_secret
    USERS_KEY_VAULT_URI: 'https://${usersKvName}${environment().suffixes.keyvaultDns}'
  }
}

resource authorize_basename_web 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  parent: authorize_basename
  name: 'web'
  properties: {
    repoUrl: repository_url
    branch: repository_branch
    isManualIntegration: true
  }
  dependsOn: [
    authorize_basename_appsettings
  ]
}

resource import_timer_basename 'Microsoft.Web/sites@2020-06-01' = {
  name: 'import-timer-${basename}'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    enabled: true
    httpsOnly: true
    serverFarmId: app_plan_basename.id
    reserved: false
    scmSiteAlsoStopped: false
    clientAffinityEnabled: true
    clientCertEnabled: false
    hostNamesDisabled: false
    containerSize: 1536
    dailyMemoryTimeQuota: 0
  }
}

resource import_timer_basename_appsettings 'Microsoft.Web/sites/config@2015-08-01' = {
  parent: import_timer_basename
  location: location
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    PROJECT: 'src/ImportTimerTrigger/FitOnFhir.ImportTimerTrigger/FitOnFhir.ImportTimerTrigger.csproj'
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
    WEBSITE_CONTENTSHARE: 'import-timer-${basename}-${take(uniqueString('import-timer-', basename), 4)}'
  }
  dependsOn: [
    identity_basename
  ]
}

resource import_timer_basename_web 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  parent: import_timer_basename
  name: 'web'
  properties: {
    repoUrl: repository_url
    branch: repository_branch
    isManualIntegration: true
  }
  dependsOn: [
    import_timer_basename_appsettings
  ]
}

resource import_data_basename 'Microsoft.Web/sites@2020-06-01' = {
  name: 'import-data-${basename}'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    enabled: true
    httpsOnly: true
    serverFarmId: app_plan_basename.id
    reserved: false
    scmSiteAlsoStopped: false
    clientAffinityEnabled: true
    clientCertEnabled: false
    hostNamesDisabled: false
    containerSize: 1536
    dailyMemoryTimeQuota: 0
  }
}

resource import_data_basename_appsettings 'Microsoft.Web/sites/config@2015-08-01' = {
  parent: import_data_basename
  location: location
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    PROJECT: 'src/Import/FitOnFhir.Import/FitOnFhir.Import.csproj'
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
    WEBSITE_CONTENTSHARE: 'import-data-${basename}-${take(uniqueString('import-data-', basename), 4)}'
    EventHubConnectionString: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', split('${infraKvName}/eventhub-connection-string', '/')[0], split('${infraKvName}/eventhub-connection-string', '/')[1])).secretUriWithVersion})'
    GOOGLE_OAUTH_CLIENT_ID: google_client_id
    GOOGLE_OAUTH_CLIENT_SECRET: google_client_secret
    USERS_KEY_VAULT_URI: 'https://${usersKvName}${environment().suffixes.keyvaultDns}'
  }
  dependsOn: [
    infraKvName_eventhub_connection_string
  ]
}

resource import_data_basename_web 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  parent: import_data_basename
  name: 'web'
  properties: {
    repoUrl: repository_url
    branch: repository_branch
    isManualIntegration: true
  }
  dependsOn: [
    import_data_basename_appsettings
  ]
}

resource en_basename 'Microsoft.EventHub/namespaces@2021-01-01-preview' = {
  name: 'en-${basename}'
  location: location
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

resource en_basename_ingest 'Microsoft.EventHub/namespaces/eventhubs@2021-01-01-preview' = {
  parent: en_basename
  name: 'ingest'
  properties: {
    messageRetentionInDays: 1
    partitionCount: 4
  }
}

resource en_basename_ingest_Default 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2021-01-01-preview' = {
  parent: en_basename_ingest
  name: '$Default'
}

resource en_basename_ingest_FunctionSender 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-01-01-preview' = {
  parent: en_basename_ingest
  name: 'FunctionSender'
  properties: {
    rights: [
      'Send'
    ]
  }
}

resource hw_basename 'Microsoft.HealthcareApis/workspaces@2021-06-01-preview' = {
  name: replace('hw-${basename}', '-', '')
  location: location
  properties: {}
}

resource hw_basename_fs_basename 'Microsoft.HealthcareApis/workspaces/fhirservices@2021-06-01-preview' = {
  name: '${replace('hw-${basename}', '-', '')}/fs-${basename}'
  location: location
  kind: 'fhir-R4'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    authenticationConfiguration: {
      authority: '${environment().authentication.loginEndpoint}${subscription().tenantId}'
      audience: 'https://${replace('hw-${basename}', '-', '')}-fs-${basename}.fhir.azurehealthcareapis.com'
      smartProxyEnabled: false
    }
  }
  dependsOn: [
    hw_basename
  ]
}

resource hw_basename_hi_basename 'Microsoft.HealthcareApis/workspaces/iotconnectors@2021-06-01-preview' = {
  name: '${replace('hw-${basename}', '-', '')}/hi-${basename}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    ingestionEndpointConfiguration: {
      eventHubName: 'ingest'
      consumerGroup: '$Default'
      fullyQualifiedEventHubNamespace: 'en-${basename}.servicebus.windows.net'
    }
    deviceMapping: {
      content: {
        templateType: 'CollectionContent'
        template: [
          {
            templateType: 'CalculatedContent'
            template: {
              typeName: 'com.google.blood_glucose'
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.blood_glucose\' && $.dataSourceId =~ /com.google.android.apps.fitness/)]'
              deviceIdExpression: '$.deviceIdentifier'
              patientIdExpression: '$.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  required: 'true'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  valueName: 'blood_glucose_level'
                }
                {
                  required: 'false'
                  valueExpression: 'matchedToken.value[1].intVal'
                  valueName: 'temporal_relation_to_meal'
                }
                {
                  required: 'false'
                  valueExpression: 'matchedToken.value[2].intVal'
                  valueName: 'meal_type'
                }
                {
                  required: 'false'
                  valueExpression: 'matchedToken.value[3].intVal'
                  valueName: 'temporal_relation_to_sleep'
                }
                {
                  required: 'false'
                  valueExpression: 'matchedToken.value[4].intVal'
                  valueName: 'blood_glucose_specimen_source'
                }
              ]
            }
          }
        ]
      }
    }
  }
  dependsOn: [
    en_basename
    en_basename_ingest_Default
    en_basename_ingest
    hw_basename
  ]
}

resource id_FhirWriter 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  scope: hw_basename_fs_basename
  name: guid('${resourceGroup().id}-FhirWriter')
  properties: {
    roleDefinitionId: '${subscription().id}/providers/Microsoft.Authorization/roleDefinitions/${fhirWriterRoleId}'
    principalId: reference(hw_basename_hi_basename.id, '2021-06-01-preview', 'Full').identity.principalId
  }
}

resource id_EventHubDataReceiver 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  scope: en_basename_ingest
  name: guid('${resourceGroup().id}-EventHubDataReceiver')
  properties: {
    roleDefinitionId: '${subscription().id}/providers/Microsoft.Authorization/roleDefinitions/${eventHubReceiverRoleId}'
    principalId: reference(hw_basename_hi_basename.id, '2021-06-01-preview', 'Full').identity.principalId
  }
}

resource hw_basename_hi_basename_hd_basename 'Microsoft.HealthcareApis/workspaces/iotconnectors/fhirdestinations@2021-06-01-preview' = {
  parent: hw_basename_hi_basename
  name: 'hd-${basename}'
  location: location
  properties: {
    resourceIdentityResolutionType: 'Create'
    fhirServiceResourceId: hw_basename_fs_basename.id
    fhirMapping: {
      content: {
        templateType: 'CollectionFhir'
        template: [
          {
            templateType: 'CodeValueFhir'
            template: {
              typeName: 'com.google.blood_glucose'
              value: {
                valueName: 'blood_glucose_level'
                valueType: 'Quantity'
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
  dependsOn: [
    hw_basename
  ]
}

output usersKeyVaultName string = usersKvName
output infraKeyVaultName string = infraKvName
output authorizeAppName string = 'authorize-${basename}'
output importTimerAppName string = 'import-timer-${basename}'
output importDataAppName string = 'import-data-${basename}'
