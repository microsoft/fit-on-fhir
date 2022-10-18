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

@description('A list of identity provider URLs used when authentication is required.')
param authentication_identity_providers string = ''

@description('The URL that any access tokens are granted for.')
param authentication_audience string = ''

@description('The Google Fit data authorization scopes allowed for users of this service (see https://developers.google.com/fit/datatypes#authorization_scopes for more info)')
param google_fit_scopes string = 'https://www.googleapis.com/auth/userinfo.profile,https://www.googleapis.com/auth/fitness.activity.read,https://www.googleapis.com/auth/fitness.sleep.read,https://www.googleapis.com/auth/fitness.reproductive_health.read,https://www.googleapis.com/auth/fitness.oxygen_saturation.read,https://www.googleapis.com/auth/fitness.nutrition.read,https://www.googleapis.com/auth/fitness.location.read,https://www.googleapis.com/auth/fitness.body_temperature.read,https://www.googleapis.com/auth/fitness.body.read,https://www.googleapis.com/auth/fitness.blood_pressure.read,https://www.googleapis.com/auth/fitness.blood_glucose.read,https://www.googleapis.com/auth/fitness.heart_rate.read'

@description('A comma delimited list of approved redirect URLs that can be navigated to when authentication completes successfully.')
param authentication_redirect_urls string

@description('An array of allowed origins that can make requests to the authorize API (CORS).')
param authorize_allowed_origins array

var fhir_data_writer = resourceId('Microsoft.Authorization/roleDefinitions', '3f88fce4-5892-4214-ae73-ba5294559913')
var event_hubs_data_receiver = resourceId('Microsoft.Authorization/roleDefinitions', 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde')
var event_hubs_data_sender = resourceId('Microsoft.Authorization/roleDefinitions', '2b629674-e913-4c01-ae53-ef4638d8f975')
var storage_table_data_contributor = resourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
var storage_blob_data_owner = resourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')
var storage_queue_data_contributor = resourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
var storage_queue_data_message_sender = resourceId('Microsoft.Authorization/roleDefinitions', 'c6a89b2d-59bc-44d0-9896-0f6e12d7b80a')
var storage_queue_data_message_processor = resourceId('Microsoft.Authorization/roleDefinitions', '8a0f0c08-91a1-4084-bc3d-661d67233fed')

resource kv_resource 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: 'kv-${basename}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        tenantId: import_data_basename.identity.tenantId
        objectId: import_data_basename.identity.principalId
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
        tenantId: import_timer_basename.identity.tenantId
        objectId: import_timer_basename.identity.principalId
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
      {
        tenantId: authorize_basename.identity.tenantId
        objectId: authorize_basename.identity.principalId
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

resource kv_storage_account_connection_string 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: kv_resource
  name: 'storage-account-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${sa_basename.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${sa_basename.listKeys().keys[0].value}'
  }
}

resource kv_google_client_secret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: kv_resource
  name: 'google-client-secret'
  properties: {
    value: google_client_secret
  }
}

resource sa_basename 'Microsoft.Storage/storageAccounts@2022-05-01' = {
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

resource sa_basename_default 'Microsoft.Storage/storageAccounts/blobServices@2022-05-01' = {
  name: 'default'
  parent: sa_basename
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
	lastAccessTimeTrackingPolicy: {
      blobType: [
        'blockBlob'
      ]
      enable: true
      name: 'AccessTimeTracking'
      trackingGranularityInDays: 1
    }
  }
}

resource sa_basename_default_management_policy 'Microsoft.Storage/storageAccounts/managementPolicies@2022-05-01' = {
  name: 'default'
  parent: sa_basename
  properties: {
    policy: {
      rules: [
        {
          definition: {
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: 1
                }
                enableAutoTierToHotFromCool: false
              }
            }
			filters: {
              blobTypes: [
                'blockBlob'
              ]
			  prefixMatch: [
				'authdata/'
			  ]
            }
          }
          enabled: true
          name: 'blob management policy'
          type: 'Lifecycle'
        }
      ]
    }
  }
}

resource sa_basename_default_blob_container 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-05-01' = {
  parent: sa_basename_default
  name: 'authdata'
}

resource Microsoft_Storage_storageAccounts_fileServices_sa_basename_default 'Microsoft.Storage/storageAccounts/fileServices@2022-05-01' = {
  name: 'default'
  parent: sa_basename
  properties: {}
}

resource Microsoft_Storage_storageAccounts_queueServices_sa_basename_default 'Microsoft.Storage/storageAccounts/queueServices@2022-05-01' = {
  name: 'default'
  parent: sa_basename
}

resource sa_basename_default_import_data 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-05-01' = {
  parent: Microsoft_Storage_storageAccounts_queueServices_sa_basename_default
  name: 'import-data'
  properties: {
    metadata: {}
  }
}

resource Microsoft_Storage_storageAccounts_tableServices_sa_basename_default 'Microsoft.Storage/storageAccounts/tableServices@2022-05-01' = {
  name: 'default'
  parent: sa_basename
}

resource sa_basename_default_users 'Microsoft.Storage/storageAccounts/tableServices/tables@2022-05-01' = {
  parent: Microsoft_Storage_storageAccounts_tableServices_sa_basename_default
  name: 'users'
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

resource app_plan_basename 'Microsoft.Web/serverfarms@2022-03-01' = {
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

resource authorize_basename 'Microsoft.Web/sites@2022-03-01' = {
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
    siteConfig: {
      cors: {
        allowedOrigins: authorize_allowed_origins
      }
      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: false
    }
  }
}

resource authorize_basename_appsettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: authorize_basename
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    PROJECT: 'src/Authorization/FitOnFhir.Authorization/Microsoft.Health.FitOnFhir.Authorization.csproj'
    AzureWebJobsStorage__accountName: sa_basename.name
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${reference(kv_storage_account_connection_string.id).secretUriWithVersion})'
    WEBSITE_CONTENTSHARE: '${authorize_basename.name}-${take(uniqueString(authorize_basename.name), 4)}'
    GoogleFitAuthorizationConfiguration__ClientId: google_client_id
    GoogleFitAuthorizationConfiguration__ClientSecret: '@Microsoft.KeyVault(SecretUri=${reference(kv_google_client_secret.id).secretUriWithVersion})'
	  GoogleFitAuthorizationConfiguration__Scopes: google_fit_scopes
    AzureConfiguration__BlobServiceUri: sa_basename.properties.primaryEndpoints.blob
    AzureConfiguration__TableServiceUri: sa_basename.properties.primaryEndpoints.table
    AzureConfiguration__QueueServiceUri: sa_basename.properties.primaryEndpoints.queue
    AzureConfiguration__VaultUri: kv_resource.properties.vaultUri
	  AuthenticationConfiguration__IsAnonymousLoginEnabled : (authentication_anonymous_login_enabled == true) ? 'true' : 'false'
	  AuthenticationConfiguration__IdentityProviders: authentication_identity_providers
	  AuthenticationConfiguration__Audience: authentication_audience
	  AuthenticationConfiguration__RedirectUrls: authentication_redirect_urls
    FhirService__Url: hw_basename_fs_basename.properties.authenticationConfiguration.audience
    FhirClient__UseManagedIdentity: 'true'
  }
}

resource authorize_basename_web 'Microsoft.Web/sites/sourcecontrols@2022-03-01' = {
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

resource import_timer_basename 'Microsoft.Web/sites@2022-03-01' = {
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
    siteConfig: {
      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: false
    }
  }
}

resource import_timer_basename_appsettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: import_timer_basename
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    PROJECT: 'src/ImportTimerTrigger/FitOnFhir.ImportTimerTrigger/Microsoft.Health.FitOnFhir.ImportTimerTrigger.csproj'
    AzureWebJobsStorage__accountName: sa_basename.name
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${reference(kv_storage_account_connection_string.id).secretUriWithVersion})'
    WEBSITE_CONTENTSHARE: '${import_timer_basename.name}-${take(uniqueString(import_timer_basename.name), 4)}'
    AzureConfiguration__TableServiceUri: sa_basename.properties.primaryEndpoints.table
    AzureConfiguration__QueueServiceUri: sa_basename.properties.primaryEndpoints.queue
    SCHEDULE: '0 0 * * * *'
  }
}

resource import_timer_basename_web 'Microsoft.Web/sites/sourcecontrols@2022-03-01' = {
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

resource import_data_basename 'Microsoft.Web/sites@2022-03-01' = {
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
    siteConfig: {
      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: false
    }
  }
}

resource import_data_basename_appsettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: import_data_basename
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    PROJECT: 'src/Import/FitOnFhir.Import/Microsoft.Health.FitOnFhir.Import.csproj'
    AzureWebJobsStorage__accountName: sa_basename.name
    AzureWebJobsStorage__queueServiceUri: sa_basename.properties.primaryEndpoints.queue
    APPINSIGHTS_INSTRUMENTATIONKEY: ai_basename.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: ai_basename.properties.ConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${reference(kv_storage_account_connection_string.id).secretUriWithVersion})'
    WEBSITE_CONTENTSHARE: '${import_data_basename.name}-${take(uniqueString(import_data_basename.name), 4)}'
    AzureConfiguration__BlobServiceUri: sa_basename.properties.primaryEndpoints.blob
    AzureConfiguration__TableServiceUri: sa_basename.properties.primaryEndpoints.table
    AzureConfiguration__QueueServiceUri: sa_basename.properties.primaryEndpoints.queue
    AzureConfiguration__EventHubFullyQualifiedNamespace: split(replace(en_basename.properties.serviceBusEndpoint, '//', ''), ':')[1]
    AzureConfiguration__EventHubName:en_basename_ingest.name
    AzureConfiguration__VaultUri: kv_resource.properties.vaultUri
    GoogleFitAuthorizationConfiguration__ClientId: google_client_id
    GoogleFitAuthorizationConfiguration__ClientSecret: '@Microsoft.KeyVault(SecretUri=${reference(kv_google_client_secret.id).secretUriWithVersion})'
	  GoogleFitAuthorizationConfiguration__Scopes: google_fit_scopes
	  GoogleFitDataImporterConfiguration__DatasetRequestLimit: google_dataset_request_limit
	  GoogleFitDataImporterConfiguration__MaxConcurrency: google_max_concurrency
    GoogleFitDataImporterConfiguration__MaxRequestsPerMinute: google_max_requests_per_minute
    GoogleFitDataImporterConfiguration__HistoricalImportTimeSpan: google_historical_import_time_span
  }
}

resource import_data_basename_web 'Microsoft.Web/sites/sourcecontrols@2022-03-01' = {
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

resource en_basename 'Microsoft.EventHub/namespaces@2021-11-01' = {
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

resource en_basename_ingest 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  parent: en_basename
  name: 'ingest'
  properties: {
    messageRetentionInDays: 1
    partitionCount: 4
  }
}

resource en_basename_ingest_Default 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2021-11-01' = {
  parent: en_basename_ingest
  name: '$Default'
}

resource en_basename_ingest_FunctionSender 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-11-01' = {
  parent: en_basename_ingest
  name: 'FunctionSender'
  properties: {
    rights: [
      'Send'
    ]
  }
}

resource hw_basename 'Microsoft.HealthcareApis/workspaces@2022-06-01' = {
  name: replace('hw-${basename}', '-', '')
  location: location
  properties: {}
}

resource hw_basename_fs_basename 'Microsoft.HealthcareApis/workspaces/fhirservices@2022-06-01' = {
  name: 'fs-${basename}'
  parent: hw_basename
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
}

resource hw_basename_hi_basename 'Microsoft.HealthcareApis/workspaces/iotconnectors@2022-06-01' = {
  name: 'hi-${basename}'
  parent: hw_basename
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
  ]
}

resource hw_basename_hi_basename_hd_basename 'Microsoft.HealthcareApis/workspaces/iotconnectors/fhirdestinations@2022-06-01' = {
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
}

resource authorize_fhir_data_writer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: hw_basename_fs_basename
  name: guid(fhir_data_writer, authorize_basename.id, hw_basename_fs_basename.id)
  properties: {
    roleDefinitionId: fhir_data_writer
    principalId: authorize_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource authorize_storage_table_data_contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_table_data_contributor, authorize_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_table_data_contributor
    principalId: authorize_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource authorize_storage_blob_data_owner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_blob_data_owner, authorize_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_blob_data_owner
    principalId: authorize_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource authorize_storage_queue_data_message_sender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_queue_data_message_sender, authorize_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_queue_data_message_sender
    principalId: authorize_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_timer_storage_table_data_contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_table_data_contributor, import_timer_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_table_data_contributor
    principalId: import_timer_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_timer_storage_blob_data_owner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_blob_data_owner, import_timer_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_blob_data_owner
    principalId: import_timer_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_timer_storage_queue_data_message_sender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_queue_data_message_sender, import_timer_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_queue_data_message_sender
    principalId: import_timer_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_data_event_hubs_data_sender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: en_basename_ingest
  name: guid(event_hubs_data_sender, import_data_basename.id, en_basename_ingest.id)
  properties: {
    roleDefinitionId: event_hubs_data_sender
    principalId: import_data_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_data_storage_table_data_contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_table_data_contributor, import_data_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_table_data_contributor
    principalId: import_data_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_data_storage_blob_data_owner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_blob_data_owner, import_data_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_blob_data_owner
    principalId: import_data_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_data_storage_queue_data_contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_queue_data_contributor, import_data_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_queue_data_contributor
    principalId: import_data_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource import_data_storage_queue_data_message_processor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sa_basename
  name: guid(storage_queue_data_message_processor, import_data_basename.id, sa_basename.id)
  properties: {
    roleDefinitionId: storage_queue_data_message_processor
    principalId: import_data_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource hw_basename_hi_basename_fhir_data_writer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: hw_basename_fs_basename
  name: guid(fhir_data_writer, hw_basename_hi_basename.id, hw_basename_fs_basename.id)
  properties: {
    roleDefinitionId: fhir_data_writer
    principalId: hw_basename_hi_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

resource hw_basename_hi_basename_event_hubs_data_receiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: en_basename_ingest
  name: guid(event_hubs_data_receiver, hw_basename_hi_basename.id, en_basename_ingest.id)
  properties: {
    roleDefinitionId: event_hubs_data_receiver
    principalId: hw_basename_hi_basename.identity.principalId
  }
  dependsOn: [
    hw_basename_hi_basename_hd_basename
  ]
}

output authorizeAppName string = 'authorize-${basename}'
output importTimerAppName string = 'import-timer-${basename}'
output importDataAppName string = 'import-data-${basename}'
