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

@description('A comma delimited list of of allowed origins that can make requests to the authorize API (CORS).')
param authorize_allowed_origins string

var fhir_data_writer = resourceId('Microsoft.Authorization/roleDefinitions', '3f88fce4-5892-4214-ae73-ba5294559913')
var event_hubs_data_receiver = resourceId('Microsoft.Authorization/roleDefinitions', 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde')
var event_hubs_data_sender = resourceId('Microsoft.Authorization/roleDefinitions', '2b629674-e913-4c01-ae53-ef4638d8f975')
var storage_table_data_contributor = resourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
var storage_blob_data_owner = resourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')
var storage_queue_data_contributor = resourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
var storage_queue_data_message_sender = resourceId('Microsoft.Authorization/roleDefinitions', 'c6a89b2d-59bc-44d0-9896-0f6e12d7b80a')
var storage_queue_data_message_processor = resourceId('Microsoft.Authorization/roleDefinitions', '8a0f0c08-91a1-4084-bc3d-661d67233fed')

resource kv_basename 'Microsoft.KeyVault/vaults@2022-07-01' = {
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
  parent: kv_basename
  name: 'storage-account-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${sa_basename.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${sa_basename.listKeys().keys[0].value}'
  }
}

resource kv_google_client_secret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: kv_basename
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
        allowedOrigins: split(replace(authorize_allowed_origins, ' ', ''), ',')
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
    AzureConfiguration__VaultUri: kv_basename.properties.vaultUri
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
    AzureConfiguration__VaultUri: kv_basename.properties.vaultUri
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
                  valueExpression: {
                    value: 'matchedToken.value[1].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_GENERAL\'},{"v":@,"n":`2`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_FASTING\'},{"v":@,"n":`3`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_BEFORE_MEAL\'},{"v":@,"n":`4`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_AFTER_MEAL\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'meal_type'
                  valueExpression: {
                    value: 'matchedToken.value[2].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'MEAL_TYPE_UNKNOWN\'},{"v":@,"n":`2`,"s":\'MEAL_TYPE_BREAKFAST\'},{"v":@,"n":`3`,"s":\'MEAL_TYPE_LUNCH\'},{"v":@,"n":`4`,"s":\'MEAL_TYPE_DINNER\'},{"v":@,"n":`5`,"s":\'MEAL_TYPE_SNACK\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'temporal_relation_to_sleep'
                  valueExpression: {
                    value: 'matchedToken.value[3].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'TEMPORAL_RELATION_TO_SLEEP_FULLY_AWAKE\'},{"v":@,"n":`2`,"s":\'TEMPORAL_RELATION_TO_SLEEP_BEFORE_SLEEP\'},{"v":@,"n":`3`,"s":\'TEMPORAL_RELATION_TO_SLEEP_ON_WAKING\'},{"v":@,"n":`4`,"s":\'TEMPORAL_RELATION_TO_SLEEP_DURING_SLEEP\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'blood_glucose_specimen_source'
                  valueExpression: {
                    value: 'matchedToken.value[4].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_INTERSTITIAL_FLUID\'},{"v":@,"n":`2`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_CAPILLARY_BLOOD\'},{"v":@,"n":`3`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_PLASMA\'},{"v":@,"n":`4`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_SERUM\'},{"v":@,"n":`5`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_TEARS\'},{"v":@,"n":`6`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_WHOLE_BLOOD\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'derived:com.google.blood_glucose:com.google.android.gms:merged'
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
                  valueExpression: {
                    value: 'matchedToken.value[2].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BODY_POSITION_STANDING\'},{"v":@,"n":`2`,"s":\'BODY_POSITION_SITTING\'},{"v":@,"n":`3`,"s":\'BODY_POSITION_LYING_DOWN\'},{"v":@,"n":`4`,"s":\'BODY_POSITION_SEMI_RECUMBENT\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'blood_pressure_measurement_location'
                  valueExpression: {
                    value: 'matchedToken.value[3].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_LEFT_WRIST\'},{"v":@,"n":`2`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_RIGHT_WRIST\'},{"v":@,"n":`3`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_LEFT_UPPER_ARM\'},{"v":@,"n":`4`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_RIGHT_UPPER_ARM\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'derived:com.google.blood_pressure:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.body.fat.percentage\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merged/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'percentage'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.body.fat.percentage:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.body.fat.percentage\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.fitkit/ && $.Body.dataSourceId =~ /apple/ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /local_data/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'percentage'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.body.fat.percentage:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.body.temperature\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merged/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'body_temperature'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
                {
                  valueName: 'body_temperature_measurement_location'
                  valueExpression: {
                    value: 'matchedToken.value[1].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_AXILLARY\'},{"v":@,"n":`2`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_FINGER\'},{"v":@,"n":`3`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_FOREHEAD\'},{"v":@,"n":`4`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_ORAL\'},{"v":@,"n":`5`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_RECTAL\'},{"v":@,"n":`6`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_TEMPORAL_ARTERY\'},{"v":@,"n":`7`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_TOE\'},{"v":@,"n":`8`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_TYMPANIC\'},{"v":@,"n":`9`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_WRIST\'},{"v":@,"n":`10`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_VAGINAL\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'derived:com.google.body.temperature:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_minutes\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merge_heart_minutes/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'intensity'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.heart_minutes:com.google.android.gms:merge_heart_minutes'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_minutes\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.ios.fit/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /top_level/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'intensity'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.heart_minutes:com.google.ios.fit:appleinc.:iphone:top_level'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_minutes\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.ios.fit/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /watch/ && $.Body.dataSourceId =~ /top_level/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'intensity'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.heart_minutes:com.google.ios.fit:appleinc.:watch:top_level'
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
              typeName: 'derived:com.google.heart_rate.bpm:com.google.android.gms:merge_heart_rate_bpm'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_rate.bpm\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /resting_heart_rate<-merge_heart_rate_bpm/)]'
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
              typeName: 'derived:com.google.heart_rate.bpm:com.google.android.gms:resting_heart_rate<-merge_heart_rate_bpm'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_rate.bpm\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.fitkit/ && $.Body.dataSourceId =~ /apple/ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /local_data/)]'
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
              typeName: 'derived:com.google.heart_rate.bpm:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_rate.bpm\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.fitkit/ && $.Body.dataSourceId =~ /apple/ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /top_level/)]'
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
              typeName: 'derived:com.google.heart_rate.bpm:com.google.fitkit:apple:iphone:top_level'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_rate.bpm\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.ios.fit/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /watch/ && $.Body.dataSourceId =~ /top_level/)]'
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
              typeName: 'derived:com.google.heart_rate.bpm:com.google.ios.fit:appleinc.:watch:top_level'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.height\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merge_height/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'height'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.height:com.google.android.gms:merge_height'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.height\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.fitkit/ && $.Body.dataSourceId =~ /apple/ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /local_data/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'height'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.height:com.google.fitkit:apple:iphone:local_data'
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
                  valueExpression: {
                    value: 'matchedToken.value[2].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_THERAPY_ADMINISTRATION_MODE_NASAL_CANULA\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'oxygen_saturation_system'
                  valueExpression: {
                    value: 'matchedToken.value[3].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_SATURATION_SYSTEM_PERIPHERAL_CAPILLARY\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'oxygen_saturation_measurement_method'
                  valueExpression: {
                    value: 'matchedToken.value[4].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_SATURATION_MEASUREMENT_METHOD_PULSE_OXIMETRY\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'derived:com.google.oxygen_saturation:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.oxygen_saturation\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.fitkit/ && $.Body.dataSourceId =~ /apple/ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /local_data/)]'
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
                  valueExpression: {
                    value: 'matchedToken.value[2].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_THERAPY_ADMINISTRATION_MODE_NASAL_CANULA\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'oxygen_saturation_system'
                  valueExpression: {
                    value: 'matchedToken.value[3].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_SATURATION_SYSTEM_PERIPHERAL_CAPILLARY\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'oxygen_saturation_measurement_method'
                  valueExpression: {
                    value: 'matchedToken.value[4].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_SATURATION_MEASUREMENT_METHOD_PULSE_OXIMETRY\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'derived:com.google.oxygen_saturation:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.step_count.delta\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /estimated_steps/)]'
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
              typeName: 'derived:com.google.step_count.delta:com.google.android.gms:estimated_steps'
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
              typeName: 'derived:com.google.step_count.delta:com.google.android.gms:merge_step_deltas'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.step_count.delta\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.ios.fit/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /top_level/)]'
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
              typeName: 'derived:com.google.step_count.delta:com.google.ios.fit:appleinc.:iphone:top_level'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.step_count.delta\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.ios.fit/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /watch/ && $.Body.dataSourceId =~ /top_level/)]'
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
              typeName: 'derived:com.google.step_count.delta:com.google.ios.fit:appleinc.:watch:top_level'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.weight\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /merge_weight/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'weight'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.weight:com.google.android.gms:merge_weight'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.weight\' && $.Body.dataSourceId =~ /derived/ && $.Body.dataSourceId =~ /com.google.fitkit/ && $.Body.dataSourceId =~ /apple/ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /local_data/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'weight'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'derived:com.google.weight:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.blood_glucose\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
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
                  valueExpression: {
                    value: 'matchedToken.value[1].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_GENERAL\'},{"v":@,"n":`2`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_FASTING\'},{"v":@,"n":`3`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_BEFORE_MEAL\'},{"v":@,"n":`4`,"s":\'FIELD_TEMPORAL_RELATION_TO_MEAL_AFTER_MEAL\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'meal_type'
                  valueExpression: {
                    value: 'matchedToken.value[2].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'MEAL_TYPE_UNKNOWN\'},{"v":@,"n":`2`,"s":\'MEAL_TYPE_BREAKFAST\'},{"v":@,"n":`3`,"s":\'MEAL_TYPE_LUNCH\'},{"v":@,"n":`4`,"s":\'MEAL_TYPE_DINNER\'},{"v":@,"n":`5`,"s":\'MEAL_TYPE_SNACK\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'temporal_relation_to_sleep'
                  valueExpression: {
                    value: 'matchedToken.value[3].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'TEMPORAL_RELATION_TO_SLEEP_FULLY_AWAKE\'},{"v":@,"n":`2`,"s":\'TEMPORAL_RELATION_TO_SLEEP_BEFORE_SLEEP\'},{"v":@,"n":`3`,"s":\'TEMPORAL_RELATION_TO_SLEEP_ON_WAKING\'},{"v":@,"n":`4`,"s":\'TEMPORAL_RELATION_TO_SLEEP_DURING_SLEEP\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'blood_glucose_specimen_source'
                  valueExpression: {
                    value: 'matchedToken.value[4].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_INTERSTITIAL_FLUID\'},{"v":@,"n":`2`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_CAPILLARY_BLOOD\'},{"v":@,"n":`3`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_PLASMA\'},{"v":@,"n":`4`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_SERUM\'},{"v":@,"n":`5`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_TEARS\'},{"v":@,"n":`6`,"s":\'BLOOD_GLUCOSE_SPECIMEN_SOURCE_WHOLE_BLOOD\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'raw:com.google.blood_glucose:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.blood_pressure\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
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
                  valueExpression: {
                    value: 'matchedToken.value[2].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BODY_POSITION_STANDING\'},{"v":@,"n":`2`,"s":\'BODY_POSITION_SITTING\'},{"v":@,"n":`3`,"s":\'BODY_POSITION_LYING_DOWN\'},{"v":@,"n":`4`,"s":\'BODY_POSITION_SEMI_RECUMBENT\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'blood_pressure_measurement_location'
                  valueExpression: {
                    value: 'matchedToken.value[3].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_LEFT_WRIST\'},{"v":@,"n":`2`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_RIGHT_WRIST\'},{"v":@,"n":`3`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_LEFT_UPPER_ARM\'},{"v":@,"n":`4`,"s":\'BLOOD_PRESSURE_MEASUREMENT_LOCATION_RIGHT_UPPER_ARM\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'raw:com.google.blood_pressure:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.body.fat.percentage\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'percentage'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'raw:com.google.body.fat.percentage:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.body.temperature\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'body_temperature'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
                {
                  valueName: 'body_temperature_measurement_location'
                  valueExpression: {
                    value: 'matchedToken.value[1].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_AXILLARY\'},{"v":@,"n":`2`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_FINGER\'},{"v":@,"n":`3`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_FOREHEAD\'},{"v":@,"n":`4`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_ORAL\'},{"v":@,"n":`5`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_RECTAL\'},{"v":@,"n":`6`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_TEMPORAL_ARTERY\'},{"v":@,"n":`7`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_TOE\'},{"v":@,"n":`8`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_TYMPANIC\'},{"v":@,"n":`9`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_WRIST\'},{"v":@,"n":`10`,"s":\'BODY_TEMPERATURE_MEASUREMENT_LOCATION_VAGINAL\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'raw:com.google.body.temperature:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_rate.bpm\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
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
              typeName: 'raw:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.heart_rate.bpm\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /watch/ && $.Body.dataSourceId =~ /from_device/)]'
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
              typeName: 'raw:com.google.heart_rate.bpm:com.google.android.gms:appleinc.:watch:from_device'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.height\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'height'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'raw:com.google.height:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.oxygen_saturation\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
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
                  valueExpression: {
                    value: 'matchedToken.value[2].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_THERAPY_ADMINISTRATION_MODE_NASAL_CANULA\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'oxygen_saturation_system'
                  valueExpression: {
                    value: 'matchedToken.value[3].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_SATURATION_SYSTEM_PERIPHERAL_CAPILLARY\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
                {
                  valueName: 'oxygen_saturation_measurement_method'
                  valueExpression: {
                    value: 'matchedToken.value[4].intVal | [{"v":@,"n":`0`,"s":\'UNSPECIFIED\'},{"v":@,"n":`1`,"s":\'OXYGEN_SATURATION_MEASUREMENT_METHOD_PULSE_OXIMETRY\'}][?v == n].s | @[0]'
                    language: 'JmesPath'
                  }
                }
              ]
              typeName: 'raw:com.google.oxygen_saturation:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.step_count.delta\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /iphone/ && $.Body.dataSourceId =~ /derive_step_deltas/)]'
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
              typeName: 'raw:com.google.step_count.delta:com.google.android.gms:appleinc.:iphone:derive_step_deltas'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.step_count.delta\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.gms/ && $.Body.dataSourceId =~ /appleinc./ && $.Body.dataSourceId =~ /watch/ && $.Body.dataSourceId =~ /derive_step_deltas/)]'
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
              typeName: 'raw:com.google.step_count.delta:com.google.android.gms:appleinc.:watch:derive_step_deltas'
            }
          }
          {
            templateType: 'CalculatedContent'
            template: {
              typeMatchExpression: '$..[?(@dataTypeName == \'com.google.weight\' && $.Body.dataSourceId =~ /raw/ && $.Body.dataSourceId =~ /com.google.android.apps.fitness/ && $.Body.dataSourceId =~ /user_input/)]'
              deviceIdExpression: '$.Body.deviceIdentifier'
              patientIdExpression: '$.Body.patientIdentifier'
              timestampExpression: {
                value: 'fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))'
                language: 'JmesPath'
              }
              values: [
                {
                  valueName: 'weight'
                  valueExpression: 'matchedToken.value[0].fpVal'
                  required: true
                }
              ]
              typeName: 'raw:com.google.weight:com.google.android.apps.fitness:user_input'
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
                  code: 'com.google.blood_glucose'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.blood_glucose_level'
                    }
                  ]
                  value: {
                    valueName: 'blood_glucose_level'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.temporal_relation_to_meal'
                    }
                  ]
                  value: {
                    valueName: 'temporal_relation_to_meal'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.meal_type'
                    }
                  ]
                  value: {
                    valueName: 'meal_type'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.temporal_relation_to_sleep'
                    }
                  ]
                  value: {
                    valueName: 'temporal_relation_to_sleep'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.blood_glucose_specimen_source'
                    }
                  ]
                  value: {
                    valueName: 'blood_glucose_specimen_source'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'derived:com.google.blood_glucose:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.blood_pressure'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.blood_pressure_systolic'
                    }
                  ]
                  value: {
                    valueName: 'blood_pressure_systolic'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.blood_pressure_diastolic'
                    }
                  ]
                  value: {
                    valueName: 'blood_pressure_diastolic'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.body_position'
                    }
                  ]
                  value: {
                    valueName: 'body_position'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.blood_pressure_measurement_location'
                    }
                  ]
                  value: {
                    valueName: 'blood_pressure_measurement_location'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'derived:com.google.blood_pressure:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.body.fat.percentage'
                }
              ]
              value: {
                valueName: 'percentage'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.body.fat.percentage:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.body.fat.percentage'
                }
              ]
              value: {
                valueName: 'percentage'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.body.fat.percentage:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.body.temperature'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.body.temperature.body_temperature'
                    }
                  ]
                  value: {
                    valueName: 'body_temperature'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.body.temperature.body_temperature_measurement_location'
                    }
                  ]
                  value: {
                    valueName: 'body_temperature_measurement_location'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'derived:com.google.body.temperature:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_minutes'
                }
              ]
              value: {
                valueName: 'intensity'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_minutes:com.google.android.gms:merge_heart_minutes'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_minutes'
                }
              ]
              value: {
                valueName: 'intensity'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_minutes:com.google.ios.fit:appleinc.:iphone:top_level'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_minutes'
                }
              ]
              value: {
                valueName: 'intensity'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_minutes:com.google.ios.fit:appleinc.:watch:top_level'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_rate.bpm'
                }
              ]
              value: {
                valueName: 'bpm'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_rate.bpm:com.google.android.gms:merge_heart_rate_bpm'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_rate.bpm'
                }
              ]
              value: {
                valueName: 'bpm'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_rate.bpm:com.google.android.gms:resting_heart_rate<-merge_heart_rate_bpm'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_rate.bpm'
                }
              ]
              value: {
                valueName: 'bpm'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_rate.bpm:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_rate.bpm'
                }
              ]
              value: {
                valueName: 'bpm'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_rate.bpm:com.google.fitkit:apple:iphone:top_level'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_rate.bpm'
                }
              ]
              value: {
                valueName: 'bpm'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.heart_rate.bpm:com.google.ios.fit:appleinc.:watch:top_level'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.height'
                }
              ]
              value: {
                valueName: 'height'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.height:com.google.android.gms:merge_height'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.height'
                }
              ]
              value: {
                valueName: 'height'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.height:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.oxygen_saturation'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.supplemental_oxygen_flow_rate'
                    }
                  ]
                  value: {
                    valueName: 'supplemental_oxygen_flow_rate'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_therapy_administration_mode'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_therapy_administration_mode'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation_system'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation_system'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation_measurement_method'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation_measurement_method'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'derived:com.google.oxygen_saturation:com.google.android.gms:merged'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.oxygen_saturation'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.supplemental_oxygen_flow_rate'
                    }
                  ]
                  value: {
                    valueName: 'supplemental_oxygen_flow_rate'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_therapy_administration_mode'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_therapy_administration_mode'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation_system'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation_system'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation_measurement_method'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation_measurement_method'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'derived:com.google.oxygen_saturation:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.step_count.delta'
                }
              ]
              value: {
                valueName: 'steps'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.step_count.delta:com.google.android.gms:estimated_steps'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.step_count.delta'
                }
              ]
              value: {
                valueName: 'steps'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.step_count.delta:com.google.android.gms:merge_step_deltas'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.step_count.delta'
                }
              ]
              value: {
                valueName: 'steps'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.step_count.delta:com.google.ios.fit:appleinc.:iphone:top_level'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.step_count.delta'
                }
              ]
              value: {
                valueName: 'steps'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.step_count.delta:com.google.ios.fit:appleinc.:watch:top_level'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.weight'
                }
              ]
              value: {
                valueName: 'weight'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.weight:com.google.android.gms:merge_weight'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.weight'
                }
              ]
              value: {
                valueName: 'weight'
                valueType: 'Quantity'
              }
              typeName: 'derived:com.google.weight:com.google.fitkit:apple:iphone:local_data'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.blood_glucose'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.blood_glucose_level'
                    }
                  ]
                  value: {
                    valueName: 'blood_glucose_level'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.temporal_relation_to_meal'
                    }
                  ]
                  value: {
                    valueName: 'temporal_relation_to_meal'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.meal_type'
                    }
                  ]
                  value: {
                    valueName: 'meal_type'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.temporal_relation_to_sleep'
                    }
                  ]
                  value: {
                    valueName: 'temporal_relation_to_sleep'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_glucose.blood_glucose_specimen_source'
                    }
                  ]
                  value: {
                    valueName: 'blood_glucose_specimen_source'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'raw:com.google.blood_glucose:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.blood_pressure'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.blood_pressure_systolic'
                    }
                  ]
                  value: {
                    valueName: 'blood_pressure_systolic'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.blood_pressure_diastolic'
                    }
                  ]
                  value: {
                    valueName: 'blood_pressure_diastolic'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.body_position'
                    }
                  ]
                  value: {
                    valueName: 'body_position'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.blood_pressure.blood_pressure_measurement_location'
                    }
                  ]
                  value: {
                    valueName: 'blood_pressure_measurement_location'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'raw:com.google.blood_pressure:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.body.fat.percentage'
                }
              ]
              value: {
                valueName: 'percentage'
                valueType: 'Quantity'
              }
              typeName: 'raw:com.google.body.fat.percentage:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.body.temperature'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.body.temperature.body_temperature'
                    }
                  ]
                  value: {
                    valueName: 'body_temperature'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.body.temperature.body_temperature_measurement_location'
                    }
                  ]
                  value: {
                    valueName: 'body_temperature_measurement_location'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'raw:com.google.body.temperature:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_rate.bpm'
                }
              ]
              value: {
                valueName: 'bpm'
                valueType: 'Quantity'
              }
              typeName: 'raw:com.google.heart_rate.bpm:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.heart_rate.bpm'
                }
              ]
              value: {
                valueName: 'bpm'
                valueType: 'Quantity'
              }
              typeName: 'raw:com.google.heart_rate.bpm:com.google.android.gms:appleinc.:watch:from_device'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.height'
                }
              ]
              value: {
                valueName: 'height'
                valueType: 'Quantity'
              }
              typeName: 'raw:com.google.height:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.oxygen_saturation'
                }
              ]
              components: [
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.supplemental_oxygen_flow_rate'
                    }
                  ]
                  value: {
                    valueName: 'supplemental_oxygen_flow_rate'
                    valueType: 'Quantity'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_therapy_administration_mode'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_therapy_administration_mode'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation_system'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation_system'
                    valueType: 'String'
                  }
                }
                {
                  codes: [
                    {
                      code: 'com.google.oxygen_saturation.oxygen_saturation_measurement_method'
                    }
                  ]
                  value: {
                    valueName: 'oxygen_saturation_measurement_method'
                    valueType: 'String'
                  }
                }
              ]
              typeName: 'raw:com.google.oxygen_saturation:com.google.android.apps.fitness:user_input'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.step_count.delta'
                }
              ]
              value: {
                valueName: 'steps'
                valueType: 'Quantity'
              }
              typeName: 'raw:com.google.step_count.delta:com.google.android.gms:appleinc.:iphone:derive_step_deltas'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.step_count.delta'
                }
              ]
              value: {
                valueName: 'steps'
                valueType: 'Quantity'
              }
              typeName: 'raw:com.google.step_count.delta:com.google.android.gms:appleinc.:watch:derive_step_deltas'
            }
          }
          {
            templateType: 'CodeValueFhir'
            template: {
              codes: [
                {
                  code: 'com.google.weight'
                }
              ]
              value: {
                valueName: 'weight'
                valueType: 'Quantity'
              }
              typeName: 'raw:com.google.weight:com.google.android.apps.fitness:user_input'
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
