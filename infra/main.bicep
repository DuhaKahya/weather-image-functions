// main.bicep

@description('Location for all resources')
param location string = 'germanywestcentral'

@description('Storage account name (moet uniek zijn, alleen lowercase)')
var storageAccountName = 'weather${uniqueString(resourceGroup().id)}'

@description('Function App name (moet uniek zijn)')
var functionAppName = 'weatherfunc${uniqueString(resourceGroup().id)}'

@description('App Service plan (consumption)')
var hostingPlanName = 'weatherplan${uniqueString(resourceGroup().id)}'

//
// Storage account
//
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

//
// Queue service + queues
//
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount
}

resource startQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: 'jobs-start'
  parent: queueService
}

resource imagesQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: 'jobs-images'
  parent: queueService
}

//
// Blob service + container
//
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount
}

resource imagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storageAccount.name}/default/images'
  properties: {
    publicAccess: 'None'
  }
  dependsOn: [
    blobService
  ]
}


//
// Hosting plan (Consumption)
//
resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

//
// Function App
//
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'UnsplashApiKey'
          value: 'X6afQXchip8nR5lcDA8gzwS1_Aj-TcCjFV4hT00IZq4'
        }
      ]
    }
  }
}

//
// Outputs
//
output storageAccountName string = storageAccount.name
output functionAppName string = functionApp.name
output queues array = [
  startQueue.name
  imagesQueue.name
]
output containerName string = imagesContainer.name


// Ik heb er heel lang aan gezeten maar mocht maar geen Function
// App aanmaken vanwege locatie policies in azure.