param(
    [string]$ResourceGroupName = "weather-rg",
    [string]$DeploymentName = "infra-deploy",
    [string]$Location = "westeurope"
)

Write-Host "Deploying infrastructure to resource group $ResourceGroupName in $Location..." -ForegroundColor Cyan

# Check of de resource group bestaat, anders aanmaken
if (-not (az group exists --name $ResourceGroupName | ConvertFrom-Json)) {
    Write-Host "Resource group does not exist. Creating..." -ForegroundColor Yellow
    az group create --name $ResourceGroupName --location $Location | Out-Null
}

# Deployment uitvoeren
az deployment group create `
  --resource-group $ResourceGroupName `
  --name $DeploymentName `
  --template-file ./infra/main.bicep `
  --parameters location=$Location `
  --verbose
