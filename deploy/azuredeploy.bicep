@description('Base name that is used to name provisioned resources. Should be alphanumeric, at least 3 characters and less than 16 characters.')
@minLength(3)
@maxLength(16)
param basename string

@description('The location where the resources(s) are deployed.')
param location string = resourceGroup().location

@description('The Google OAuth2 web application client id.')
param google_client_id string

@description('The Google OAuth2 web application client secret.')
@secure()
param google_client_secret string

@description('The repository where the fit-on-fhir source code resides.')
param repository_url string = 'https://github.com/Microsoft/fit-on-fhir'

@description('The source code branch to be deployed.')
param repository_branch string = 'main'

@description('The maximum Google Fit data points returned per dataset request.')
param google_dataset_request_limit string = '1000'

@description('The maximum concurrent tasks allowed per Google Fit dataset request.')
param google_max_concurrency string = '10'

@description('The maximum number of requests that can be made to the Google APIs in a one minute period.')
param google_max_requests_per_minute string = '300'

@description('The period of time from now into the past, that the first Google Fit data import should cover.  30 days prior is the default.  Format is days.hours:minutes:seconds')
param google_historical_import_time_span string = '30.00:00:00'

@description('Enables anonymous logins (true) or requires authentication (false).')
param authentication_anonymous_login_enabled bool = false

@description('Name for the authentication data storage container.')
param authentication_blob_container_name string = 'authdata'

@description('A list of identity provider URLs used when authentication is required.')
param authentication_identity_providers string = ''

@description('The URL that any access tokens are granted for.')
param authentication_audience string = ''

@description('The Google Fit data authorization scopes allowed for users of this service (see https://developers.google.com/fit/datatypes#authorization_scopes for more info)')
param google_fit_scopes string = 'https://www.googleapis.com/auth/userinfo.email,https://www.googleapis.com/auth/userinfo.profile,https://www.googleapis.com/auth/fitness.activity.read,https://www.googleapis.com/auth/fitness.sleep.read,https://www.googleapis.com/auth/fitness.reproductive_health.read,https://www.googleapis.com/auth/fitness.oxygen_saturation.read,https://www.googleapis.com/auth/fitness.nutrition.read,https://www.googleapis.com/auth/fitness.location.read,https://www.googleapis.com/auth/fitness.body_temperature.read,https://www.googleapis.com/auth/fitness.body.read,https://www.googleapis.com/auth/fitness.blood_pressure.read,https://www.googleapis.com/auth/fitness.blood_glucose.read,https://www.googleapis.com/auth/fitness.heart_rate.read'

var fhirServiceUrl = 'https://${replace('hw-${basename}', '-', '')}-fs-${basename}.fhir.azurehealthcareapis.com'
var fhirWriterRoleId = '3f88fce4-5892-4214-ae73-ba5294559913'
var eventHubReceiverRoleId = 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde'

resource kv_resource 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: 'kv-${basename}'
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
            'get'
            'set'
            'delete'
            'list'      
          ]
        }
      }
      {
        tenantId: reference(import_timer_basename.id, '2020-06-01', 'Full').identity.tenantId
        objectId: reference(import_timer_basename.id, '2020-06-01', 'Full').identity.principalId
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
      {
        tenantId: reference(authorize_basename.id, '2020-06-01', 'Full').identity.tenantId
        objectId: reference(authorize_basename.id, '2020-06-01', 'Full').identity.principalId
        permissions: {
          secrets: [
            'get'
            'set'
            'delete'
            'list' 
          ]
        }
      }
    ]
    tenantId: subscription().tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 30
  }
}

resource kv_eventhub_connection_string 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: kv_resource
  name: 'eventhub-connection-string'
  properties: {
    value: listkeys(en_basename_ingest_FunctionSender.id, '2021-01-01-preview').primaryConnectionString
  }
}

resource kv_google_client_secret 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: kv_resource
  name: 'google-client-secret'
  properties: {
    value: google_client_secret
  }
}

resource kv_storage_account_connection_string 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  parent: kv_resource
  name: 'storage-account-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${replace('sa-${basename}', '-', '')};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(sa_basename.id, '2021-02-01').keys[0].value}'
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

resource sa_basename_default_auth_state 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = {
  name: authentication_blob_container_name
  parent: sa_basename_default
  properties: {
    publicAccess: 'None'
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
	  AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    AzureConfiguration__StorageAccountConnectionString: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    WEBSITE_CONTENTSHARE: 'authorize-${basename}-${take(uniqueString('authorize-', basename), 4)}'
    GoogleFitAuthorizationConfiguration__ClientId: google_client_id
    GoogleFitAuthorizationConfiguration__ClientSecret: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'google-client-secret')).secretUriWithVersion})'
	  GoogleFitAuthorizationConfiguration__Scopes: google_fit_scopes
    AzureConfiguration__UsersKeyVaultUri: 'https://kv-${basename}${environment().suffixes.keyvaultDns}'
	AzureConfiguration__BlobContainerName: authentication_blob_container_name
	AuthenticationConfiguration__IsAnonymousLoginEnabled : (authentication_anonymous_login_enabled == true) ? 'true' : 'false'
	AuthenticationConfiguration__IdentityProviders: (authentication_anonymous_login_enabled == true) ? '' : authentication_identity_providers
	AuthenticationConfiguration__Audience: (authentication_anonymous_login_enabled == true) ? '' : authentication_audience
    FhirService__Url: fhirServiceUrl
    FhirClient__UseManagedIdentity: 'true'
  }
  dependsOn: [
    kv_google_client_secret
    kv_storage_account_connection_string
  ]
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
    AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    AzureConfiguration__StorageAccountConnectionString: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    WEBSITE_CONTENTSHARE: 'import-timer-${basename}-${take(uniqueString('import-timer-', basename), 4)}'
    SCHEDULE: '0 0 * * * *'
  }
  dependsOn: [
    kv_storage_account_connection_string
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
    AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    AzureConfiguration__StorageAccountConnectionString: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'storage-account-connection-string')).secretUriWithVersion})'
    WEBSITE_CONTENTSHARE: 'import-data-${basename}-${take(uniqueString('import-data-', basename), 4)}'
    AzureConfiguration__EventHubConnectionString: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'eventhub-connection-string')).secretUriWithVersion})'
    GoogleFitAuthorizationConfiguration__ClientId: google_client_id
    GoogleFitAuthorizationConfiguration__ClientSecret: '@Microsoft.KeyVault(SecretUri=${reference(resourceId('Microsoft.KeyVault/vaults/secrets', 'kv-${basename}', 'google-client-secret')).secretUriWithVersion})'
	  GoogleFitAuthorizationConfiguration__Scopes: google_fit_scopes
	  GoogleFitDataImporterConfiguration__DatasetRequestLimit: google_dataset_request_limit
	  GoogleFitDataImporterConfiguration__MaxConcurrency: google_max_concurrency
    GoogleFitDataImporterConfiguration__MaxRequestsPerMinute: google_max_requests_per_minute
    GoogleFitDataImporterConfiguration__HistoricalImportTimeSpan: google_historical_import_time_span
    AzureConfiguration__UsersKeyVaultUri: 'https://kv-${basename}${environment().suffixes.keyvaultDns}'
  }
  dependsOn: [
    kv_eventhub_connection_string
    kv_google_client_secret
    kv_storage_account_connection_string
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
      audience: fhirServiceUrl
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
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.blood_glucose\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merged/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'blood_glucose_level'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
                {
                  valueName: 'temporal_relation_to_meal'
                  valueExpression: 'matchedToken.value[1].intVal'
                }
                {
                  valueName: 'meal_type'
                  valueExpression: 'matchedToken.value[2].intVal'
                }
                {
                  valueName: 'temporal_relation_to_sleep'
                  valueExpression: 'matchedToken.value[3].intVal'
                }
                {
                  valueName: 'blood_glucose_specimen_source'
                  valueExpression: 'matchedToken.value[4].intVal'
                }
              ]
              typeName: '3ag4h5u7xcmu69zxupcx'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.blood_pressure\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merged/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'blood_pressure_systolic'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
                {
                  valueName: 'blood_pressure_diastolic'
                  valueExpression: 'matchedToken.value[1].fpVal'
                  required: true
                }
                {
                  valueName: 'body_position'
                  valueExpression: 'matchedToken.value[2].intVal'
                }
                {
                  valueName: 'blood_pressure_measurement_location'
                  valueExpression: 'matchedToken.value[3].intVal'
                }
              ]
              typeName: 'lwo9eoim0lfnurbfdlfk'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_rate.bpm\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merge_heart_rate_bpm/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'bpm'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'qucm2rppd42yod4rpt14'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.oxygen_saturation\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merged/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'oxygen_saturation'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
                {
                  valueName: 'supplemental_oxygen_flow_rate'
                  valueExpression: 'matchedToken.value[1].fpVal'
                  required: true
                }
                {
                  valueName: 'oxygen_therapy_administration_mode'
                  valueExpression: 'matchedToken.value[2].intVal'
                }
                {
                  valueName: 'oxygen_saturation_system'
                  valueExpression: 'matchedToken.value[3].intVal'
                }
                {
                  valueName: 'oxygen_saturation_measurement_method'
                  valueExpression: 'matchedToken.value[4].intVal'
                }
              ]
              typeName: 'h3yjzapqywycu85xk8g0'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.step_count.delta\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merge_step_deltas/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'steps'
                  valueExpression: 'matchedToken.value[0].intVal'
                  required: true
                }
              ]
              typeName: 'ioaj1bdvxoztpyjgwufq'
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

resource auth_FhirWriter 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  scope: hw_basename_fs_basename
  name: guid('${resourceGroup().id}-AuthFhirWriter')
  properties: {
    roleDefinitionId: '${subscription().id}/providers/Microsoft.Authorization/roleDefinitions/${fhirWriterRoleId}'
    principalId: reference(authorize_basename.id, '2022-03-01', 'Full').identity.principalId
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
              codes: [
                {
                  code: '15074-8'
                  display: 'Glucose [Moles/volume] in Blood'
                  system: 'http://loinc.org'
                }
                {
                  code: '434912009'
                  display: 'Blood glucose concentration'
                  system: 'http://snomed.info/sct'
                }
              ]
              typeName: '3ag4h5u7xcmu69zxupcx'
              value: {
                valueName: 'blood_glucose_level'
                valueType: 'Quantity'
                code: 'mmol/L'
                unit: 'mmol/L'
                system: 'http://loinc.org'
              }
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: '85354-9'
                  display: 'Blood pressure panel'
                  system: 'http://loinc.org'
                }
                {
                  code: '75367002'
                  display: 'Blood pressure'
                  system: 'http://snomed.info/sct'
                }
              ]
              typeName: 'lwo9eoim0lfnurbfdlfk'
              components: [
                {
                  codes: [
                    {
                      code: '8867-4'
                      display: 'Diastolic blood pressure'
                      system: 'http://loinc.org'
                    }
                    {
                      code: '271650006'
                      display: 'Diastolic blood pressure'
                      system: 'http://snomed.info/sct'
                    }
                  ]
                  value: {
                    system: 'http://unitsofmeasure.org'
                    code: 'mmHg'
                    unit: 'mmHg'
                    valueName: 'blood_pressure_diastolic'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: '8480-6'
                      display: 'Systolic blood pressure'
                      system: 'http://loinc.org'
                    }
                    {
                      code: '271649006'
                      display: 'Systolic blood pressure'
                      system: 'http://snomed.info/sct'
                    }
                  ]
                  value: {
                    system: 'http://unitsofmeasure.org'
                    code: 'mmHg'
                    unit: 'mmHg'
                    valueName: 'blood_pressure_systolic'
                    valueType: 'Quantity'
                  }
                }
              ]
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: '8867-4'
                  system: 'http://loinc.org'
                  display: 'Heart rate'
                }
                {
                  code: '364075005'
                  system: 'http://snomed.info/sct'
                  display: 'Heart rate'
                }
              ]
              typeName: 'qucm2rppd42yod4rpt14'
              value: {
                system: 'http://unitsofmeasure.org'
                code: 'count/min'
                unit: 'count/min'
                valueName: 'bpm'
                valueType: 'Quantity'
              }
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  system: 'http://loinc.org'
                  code: '2708-6'
                  display: 'Oxygen saturation in Arterial blood'
                }
                {
                  system: 'http://snomed.info/sct'
                  code: '103228002'
                  display: 'Hemoglobin saturation with oxygen '
                }
              ]
              typeName: 'h3yjzapqywycu85xk8g0'
              value: {
                system: 'http://unitsofmeasure.org'
                code: '%'
                unit: '%'
                valueName: 'oxygen_saturation'
                valueType: 'Quantity'
              }
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: '55423-8'
                  system: 'http://loinc.org'
                  display: 'Number of steps'
                }
              ]
              typeName: 'ioaj1bdvxoztpyjgwufq'
              value: {
                system: 'http://unitsofmeasure.org'
                code: 'count'
                unit: 'count'
                valueName: 'steps'
                valueType: 'Quantity'
              }
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

output authorizeAppName string = 'authorize-${basename}'
output importTimerAppName string = 'import-timer-${basename}'
output importDataAppName string = 'import-data-${basename}'
