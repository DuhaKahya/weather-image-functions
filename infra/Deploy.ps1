param(
    [string]$ResourceGroupName = "weather-rg",
    [string]$Location = "germanywestcentral",
    [string]$DeploymentName = "infra-deploy"
)

Write-Host "Deploying infrastructure to resource group $ResourceGroupName in $Location..."

# Resourcegroep aanmaken als die nog niet bestaat
az group create `
  --name $ResourceGroupName `
  --location $Location `
  --output none

# Deploy uitvoeren (main.bicep zit in dezelfde map)
az deployment group create `
  --resource-group $ResourceGroupName `
  --template-file ".\main.bicep" `
  --name $DeploymentName `
  --parameters location=$Location `
  --verbose
