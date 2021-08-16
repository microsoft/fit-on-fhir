@description('Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.')
@minLength(3)
@maxLength(16)
param basename string = 'gfit2fhirdurable'

@description('Service prinicipal ID to give permissions for key vaults.')
param spid string

resource durableKeyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: '${basename}-kv'
  location: resourceGroup().location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
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
    softDeleteRetentionInDays: 90
  }
}

output durableKeyVaultName string = durableKeyVault.name
