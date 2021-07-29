@description('Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.')
@minLength(3)
@maxLength(16)
param basename string = 'fitonfhir'

var vKeyVaultName = 'kv-${basename}'
var vFhirServiceName = 'ha-${basename}'
var vIomtConnectorName = 'im-${basename}'
var vIomtConnectionName = 'ic-${basename}'

resource fhirService 'Microsoft.HealthcareApis/services@2021-01-11' = {
  name: vFhirServiceName
  location: resourceGroup().location
  kind: 'fhir-R4'
  properties: {
    authenticationConfiguration: {
      audience: 'https://${vFhirServiceName}.azurehealthcareapis.com'
      authority: uri(environment().authentication.loginEndpoint, subscription().tenantId)
    }
  }
}

resource iomtConnector 'Microsoft.HealthcareApis/services/iomtconnectors@2020-05-01-preview' = {
  name: vIomtConnectorName
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
  name: vIomtConnectionName
  location: resourceGroup().location
  dependsOn: [
    iomtConnector
  ]
}

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: vKeyVaultName
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

resource iomtConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2021-04-01-preview' = {
  name: '${vKeyVaultName}/eventHubConnStr'
  dependsOn: [
    keyVault
    iomtConnection
  ]
  properties: {
    value: listkeys(iomtConnection.id, '2020-05-01-preview').primaryConnectionString
    contentType: 'string'
  }
}

output keyVaultName string = keyVault.name
