@description('Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.')
@minLength(3)
@maxLength(16)
param basename string = 'fitonfhir'

resource fhirService 'Microsoft.HealthcareApis/services@2021-01-11' = {
  name: 'ha-${basename}'
  location: resourceGroup().location
  kind: 'fhir-R4'
  properties: {
    authenticationConfiguration: {
      audience: 'https://ha-${basename}.azurehealthcareapis.com'
      authority: uri(environment().authentication.loginEndpoint, subscription().tenantId)
    }
  }
}

resource iomtConnector 'Microsoft.HealthcareApis/services/iomtconnectors@2020-05-01-preview' = {
  name: '${fhirService.name}/im-${basename}'
  location: resourceGroup().location
  dependsOn: [
    fhirService
  ]
  properties: {
    serviceConfiguration: {
      resourceIdentityResolutiontype: 'Create'
    }
  }
}

resource iomtConnection 'Microsoft.HealthcareApis/services/iomtconnectors/connections@2020-05-01-preview' = {
  name: '${iomtConnector.name}/ic-${basename}'
  location: resourceGroup().location
  dependsOn: [
    iomtConnector
  ]
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

resource iomtConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2021-04-01-preview' = {
  name: '${infraKeyVault.name}/eventHubConnStr'
  dependsOn: [
    infraKeyVault
    iomtConnection
  ]
  properties: {
    value: listkeys(iomtConnection.id, '2020-05-01-preview').primaryConnectionString
    contentType: 'string'
  }
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

output usersKeyVaultName string = usersKeyVault.name
output infraKeyVaultName string = infraKeyVault.name
