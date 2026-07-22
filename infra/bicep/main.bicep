targetScope = 'resourceGroup'

@description('Short environment name such as dev, test, or prod.')
param environmentName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@secure()
@description('Bootstrap PostgreSQL administrator password. Store this in a secure deployment system.')
param postgresAdministratorPassword string

@description('Container image for the API. CI validates images but this repository does not deploy them.')
param apiImage string

@description('Container image for the worker.')
param workerImage string

var suffix = uniqueString(resourceGroup().id, environmentName)
var prefix = 'matterharbor-${environmentName}'

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: '${prefix}-identity'
  location: location
}

resource logs 'Microsoft.OperationalInsights/workspaces@2025-07-01' = {
  name: '${prefix}-logs'
  location: location
  properties: {
    retentionInDays: 30
    sku: { name: 'PerGB2018' }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: '${prefix}-sb-${suffix}'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
}

resource notifications 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBus
  name: 'matterharbor-notifications'
  properties: {
    deadLetteringOnMessageExpiration: true
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: 'oc${environmentName}${suffix}'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource vault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: '${prefix}-kv-${suffix}'
  location: location
  properties: {
    enableRbacAuthorization: true
    enableSoftDelete: true
    tenantId: tenant().tenantId
    sku: { family: 'A', name: 'standard' }
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${prefix}-pg-${suffix}'
  location: location
  sku: { name: 'Standard_B1ms', tier: 'Burstable' }
  properties: {
    administratorLogin: 'matterharboradmin'
    administratorLoginPassword: postgresAdministratorPassword
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Enabled'
      tenantId: tenant().tenantId
    }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
    storage: { storageSizeGB: 32 }
    version: '17'
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: 'matterharbor'
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
}

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: '${prefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource api 'Microsoft.App/containerApps@2025-01-01' = {
  name: '${prefix}-api'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'http' }
    }
    template: {
      containers: [{
        name: 'api'
        image: apiImage
        env: [
          { name: 'Messaging__ServiceBus__FullyQualifiedNamespace', value: '${serviceBus.name}.servicebus.windows.net' }
          { name: 'Messaging__ServiceBus__QueueName', value: notifications.name }
          { name: 'OpenTelemetry__OtlpEndpoint', value: '' }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
      }]
      scale: { minReplicas: 1, maxReplicas: 3 }
    }
  }
}

resource worker 'Microsoft.App/containerApps@2025-01-01' = {
  name: '${prefix}-worker'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environment.id
    configuration: { activeRevisionsMode: 'Single' }
    template: {
      containers: [{
        name: 'worker'
        image: workerImage
        env: [
          { name: 'Messaging__ServiceBus__FullyQualifiedNamespace', value: '${serviceBus.name}.servicebus.windows.net' }
          { name: 'Messaging__ServiceBus__QueueName', value: notifications.name }
        ]
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
      }]
      scale: { minReplicas: 1, maxReplicas: 3 }
    }
  }
}

output apiHostname string = api.properties.configuration.ingress.fqdn
