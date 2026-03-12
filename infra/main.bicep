@description('TürkAI — Azure SaaS AI Platform Infrastructure')
@description('Deploys: App Service Plan, Blazor Web App, API Web App, Azure Functions, Azure AI Foundry (Azure OpenAI), Translator, Speech, Language, Computer Vision, Personalizer, Video Indexer Account, Application Insights, Key Vault')

@minLength(3)
@maxLength(20)
param projectName string = 'turkai'

param location string = resourceGroup().location

@allowed(['Standard_LRS', 'Standard_GRS'])
param storageSkuName string = 'Standard_LRS'

@allowed(['B1', 'B2', 'B3', 'S1', 'S2', 'S3', 'P1v3', 'P2v3'])
param appServiceSkuName string = 'B2'

param azureOpenAILocation string = 'eastus'

var resourceSuffix = uniqueString(resourceGroup().id)
var shortSuffix = substring(resourceSuffix, 0, 8)

// ── Key Vault ────────────────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${projectName}-kv-${shortSuffix}'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

// ── Storage Account (Functions + Video Indexer) ───────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${projectName}sa${shortSuffix}'
  location: location
  kind: 'StorageV2'
  sku: { name: storageSkuName }
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// ── Application Insights ──────────────────────────────────────────────────────
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${projectName}-law-${shortSuffix}'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${projectName}-ai-${shortSuffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// ── App Service Plan ──────────────────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${projectName}-asp-${shortSuffix}'
  location: location
  sku: { name: appServiceSkuName }
  kind: 'linux'
  properties: { reserved: true }
}

// ── Blazor Web App ────────────────────────────────────────────────────────────
resource blazorWebApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${projectName}-web-${shortSuffix}'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appSettings: [
        { name: 'APPINSIGHTS_INSTRUMENTATIONKEY', value: appInsights.properties.InstrumentationKey }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'TurkAIApi__BaseUrl', value: 'https://${apiWebApp.properties.defaultHostName}' }
      ]
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// ── API Web App ───────────────────────────────────────────────────────────────
resource apiWebApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${projectName}-api-${shortSuffix}'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appSettings: [
        { name: 'APPINSIGHTS_INSTRUMENTATIONKEY', value: appInsights.properties.InstrumentationKey }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'AllowedOrigins', value: 'https://${blazorWebApp.properties.defaultHostName}' }
        { name: 'AzureOpenAI__Endpoint', value: azureOpenAI.properties.endpoint }
        { name: 'AzureOpenAI__DeploymentName', value: gpt4oDeployment.name }
        { name: 'AzureTranslator__Region', value: location }
        { name: 'AzureLanguage__Endpoint', value: languageService.properties.endpoint }
        { name: 'AzureVision__Endpoint', value: computerVision.properties.endpoint }
        { name: 'AzureVideoIndexer__Location', value: location }
        { name: 'AzurePersonalizer__Endpoint', value: personalizer.properties.endpoint }
      ]
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// ── Azure Functions ───────────────────────────────────────────────────────────
resource functionsApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${projectName}-fn-${shortSuffix}'
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'APPINSIGHTS_INSTRUMENTATIONKEY', value: appInsights.properties.InstrumentationKey }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'AzureOpenAI__Endpoint', value: azureOpenAI.properties.endpoint }
        { name: 'AzureOpenAI__DeploymentName', value: gpt4oDeployment.name }
        { name: 'AzureVideoIndexer__Location', value: location }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// ── Azure OpenAI (AI Foundry) ─────────────────────────────────────────────────
resource azureOpenAI 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: '${projectName}-oai-${shortSuffix}'
  location: azureOpenAILocation
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: '${projectName}-oai-${shortSuffix}'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: azureOpenAI
  name: 'gpt-4o'
  sku: { name: 'GlobalStandard', capacity: 40 }
  properties: {
    model: { format: 'OpenAI', name: 'gpt-4o', version: '2024-08-06' }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

// ── Azure AI Translator ───────────────────────────────────────────────────────
resource translator 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: '${projectName}-tr-${shortSuffix}'
  location: location
  kind: 'TextTranslation'
  sku: { name: 'S1' }
  properties: {
    customSubDomainName: '${projectName}-tr-${shortSuffix}'
    publicNetworkAccess: 'Enabled'
  }
}

// ── Azure AI Speech ───────────────────────────────────────────────────────────
resource speech 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: '${projectName}-sp-${shortSuffix}'
  location: location
  kind: 'SpeechServices'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: '${projectName}-sp-${shortSuffix}'
    publicNetworkAccess: 'Enabled'
  }
}

// ── Azure AI Language ─────────────────────────────────────────────────────────
resource languageService 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: '${projectName}-lang-${shortSuffix}'
  location: location
  kind: 'TextAnalytics'
  sku: { name: 'S' }
  properties: {
    customSubDomainName: '${projectName}-lang-${shortSuffix}'
    publicNetworkAccess: 'Enabled'
  }
}

// ── Azure Computer Vision ─────────────────────────────────────────────────────
resource computerVision 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: '${projectName}-cv-${shortSuffix}'
  location: location
  kind: 'ComputerVision'
  sku: { name: 'S1' }
  properties: {
    customSubDomainName: '${projectName}-cv-${shortSuffix}'
    publicNetworkAccess: 'Enabled'
  }
}

// ── Azure AI Personalizer ─────────────────────────────────────────────────────
resource personalizer 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: '${projectName}-pers-${shortSuffix}'
  location: location
  kind: 'Personalizer'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: '${projectName}-pers-${shortSuffix}'
    publicNetworkAccess: 'Enabled'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output blazorWebAppUrl string = 'https://${blazorWebApp.properties.defaultHostName}'
output apiWebAppUrl string = 'https://${apiWebApp.properties.defaultHostName}'
output functionsAppUrl string = 'https://${functionsApp.properties.defaultHostName}'
output azureOpenAIEndpoint string = azureOpenAI.properties.endpoint
output translatorEndpoint string = translator.properties.endpoint
output speechEndpoint string = speech.properties.endpoint
output languageEndpoint string = languageService.properties.endpoint
output visionEndpoint string = computerVision.properties.endpoint
output personalizerEndpoint string = personalizer.properties.endpoint
output keyVaultName string = keyVault.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
