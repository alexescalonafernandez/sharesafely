Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

param(
    [string]$ResourceGroupName = "rg-sharesafely-dev-we-01",
    [string]$WebAppName = "app-sharesafely-dev-we-01",
    [string]$GitHubOwner = "alexescalonafernandez",
    [string]$GitHubRepo = "sharesafely",
    [string]$GitHubEnvironment = "dev",
    [string]$AppRegistrationName = "sp-sharesafely-github-actions-dev",
    [string]$FederatedCredentialName = "github-oidc-dev",
    [string]$RoleName = "Contributor"
)

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Assert-AzureCliAvailable {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        throw "Azure CLI (az) was not found in PATH. Install Azure CLI and try again."
    }
}

function Get-AzCliValue {
    param([string]$Command)

    $value = Invoke-Expression $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed: $Command"
    }

    return "$value".Trim()
}

$checks = @()
function Add-Check {
    param([string]$Name,[bool]$Passed,[string]$Detail)
    $script:checks += [pscustomobject]@{ Name = $Name; Passed = $Passed; Detail = $Detail }
}

Write-Step "Checking Azure CLI and login"
Assert-AzureCliAvailable
$null = Get-AzCliValue "az account show --query id -o tsv"
$subscriptionId = Get-AzCliValue "az account show --query id -o tsv"
$tenantId = Get-AzCliValue "az account show --query tenantId -o tsv"
$webAppScope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$WebAppName"
$expectedSubject = "repo:$GitHubOwner/$GitHubRepo`:environment:$GitHubEnvironment"
$expectedIssuer = "https://token.actions.githubusercontent.com"
$expectedAudience = "api://AzureADTokenExchange"

Write-Step "Running Azure-side validation checks"

$webAppId = Get-AzCliValue "az webapp show --resource-group `"$ResourceGroupName`" --name `"$WebAppName`" --query id -o tsv"
Add-Check -Name "Web App exists" -Passed ([bool]$webAppId) -Detail "Web App: $WebAppName"

$appId = Get-AzCliValue "az ad app list --display-name `"$AppRegistrationName`" --query `[0].appId` -o tsv"
Add-Check -Name "App Registration exists" -Passed ([bool]$appId) -Detail "Display name: $AppRegistrationName"

$spObjectId = ""
if ($appId) {
    $spObjectId = Get-AzCliValue "az ad sp list --filter `"appId eq '$appId'`" --query `[0].id` -o tsv"
}
Add-Check -Name "Service Principal exists" -Passed ([bool]$spObjectId) -Detail "Associated to app registration"

$existingFc = $null
if ($appId) {
    $fcJson = Get-AzCliValue "az ad app federated-credential list --id `"$appId`" -o json"
    $fcList = @()
    if ($fcJson) { $fcList = $fcJson | ConvertFrom-Json }
    $existingFc = $fcList | Where-Object { $_.name -eq $FederatedCredentialName }
}

Add-Check -Name "Federated credential exists" -Passed ([bool]$existingFc) -Detail "Name: $FederatedCredentialName"

if ($existingFc) {
    Add-Check -Name "Federated credential subject matches" -Passed ($existingFc.subject -eq $expectedSubject) -Detail "Expected: $expectedSubject"
    Add-Check -Name "Federated credential issuer matches" -Passed ($existingFc.issuer -eq $expectedIssuer) -Detail "Expected: $expectedIssuer"
    Add-Check -Name "Federated credential audience contains AzureADTokenExchange" -Passed ($existingFc.audiences -contains $expectedAudience) -Detail "Expected audience: $expectedAudience"
} else {
    Add-Check -Name "Federated credential subject matches" -Passed $false -Detail "Federated credential not found"
    Add-Check -Name "Federated credential issuer matches" -Passed $false -Detail "Federated credential not found"
    Add-Check -Name "Federated credential audience contains AzureADTokenExchange" -Passed $false -Detail "Federated credential not found"
}

$rbacOk = $false
if ($spObjectId) {
    $rbacCount = Get-AzCliValue "az role assignment list --assignee-object-id `"$spObjectId`" --scope `"$webAppScope`" --query `[?roleDefinitionName=='$RoleName'] | length(@)` -o tsv"
    $rbacOk = $rbacCount -ne '0'
}
Add-Check -Name "Contributor role assignment exists at Web App scope" -Passed $rbacOk -Detail "Scope: $webAppScope"

Write-Step "Validation results"
$failed = $checks | Where-Object { -not $_.Passed }
foreach ($check in $checks) {
    if ($check.Passed) {
        Write-Host "[PASS] $($check.Name) - $($check.Detail)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $($check.Name) - $($check.Detail)" -ForegroundColor Red
    }
}

Write-Step "Manual GitHub checklist reminder"
Write-Host "- GitHub Environment '$GitHubEnvironment' exists."
Write-Host "- Environment secrets configured: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID."
Write-Host "- Environment variables configured: AZURE_WEBAPP_NAME, DOTNET_VERSION, PROJECT_PATH."
Write-Host ""
Write-Host "Resolved tenant/subscription (for reference):"
Write-Host "  AZURE_TENANT_ID = $tenantId"
Write-Host "  AZURE_SUBSCRIPTION_ID = $subscriptionId"

if ($failed.Count -gt 0) {
    Write-Host "`nAzure-side validation failed. See checklist above." -ForegroundColor Red
    exit 1
}

Write-Host "`nAzure-side validation passed." -ForegroundColor Green
exit 0
