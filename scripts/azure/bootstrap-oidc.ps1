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

Write-Step "Checking Azure CLI availability"
Assert-AzureCliAvailable

Write-Step "Checking Azure login session"
$null = Get-AzCliValue "az account show --query id -o tsv"

$subscriptionId = Get-AzCliValue "az account show --query id -o tsv"
$tenantId = Get-AzCliValue "az account show --query tenantId -o tsv"

$webAppScope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$WebAppName"
$expectedSubject = "repo:$GitHubOwner/$GitHubRepo`:environment:$GitHubEnvironment"
$expectedIssuer = "https://token.actions.githubusercontent.com"
$expectedAudience = "api://AzureADTokenExchange"

Write-Step "Verifying Web App exists"
$webAppId = Get-AzCliValue "az webapp show --resource-group `"$ResourceGroupName`" --name `"$WebAppName`" --query id -o tsv"
if (-not $webAppId) {
    throw "Web App '$WebAppName' in resource group '$ResourceGroupName' was not found."
}

Write-Step "Resolving App Registration '$AppRegistrationName'"
$appId = Get-AzCliValue "az ad app list --display-name `"$AppRegistrationName`" --query `[0].appId` -o tsv"

if (-not $appId) {
    Write-Host "App Registration not found. Creating '$AppRegistrationName'..." -ForegroundColor Yellow
    $appId = Get-AzCliValue "az ad app create --display-name `"$AppRegistrationName`" --query appId -o tsv"
} else {
    Write-Host "App Registration already exists (appId hidden)." -ForegroundColor Green
}

Write-Step "Ensuring Service Principal exists"
$spObjectId = Get-AzCliValue "az ad sp list --filter `"appId eq '$appId'`" --query `[0].id` -o tsv"
if (-not $spObjectId) {
    Write-Host "Service Principal not found. Creating..." -ForegroundColor Yellow
    $spObjectId = Get-AzCliValue "az ad sp create --id `"$appId`" --query id -o tsv"
} else {
    Write-Host "Service Principal already exists." -ForegroundColor Green
}

Write-Step "Validating federated credential '$FederatedCredentialName'"
$fcJson = Get-AzCliValue "az ad app federated-credential list --id `"$appId`" -o json"
$fcList = @()
if ($fcJson) { $fcList = $fcJson | ConvertFrom-Json }
$existingFc = $fcList | Where-Object { $_.name -eq $FederatedCredentialName }

if ($existingFc) {
    $issuerMatches = $existingFc.issuer -eq $expectedIssuer
    $subjectMatches = $existingFc.subject -eq $expectedSubject
    $audienceMatches = $existingFc.audiences -contains $expectedAudience

    if (-not ($issuerMatches -and $subjectMatches -and $audienceMatches)) {
        throw @"
Federated credential '$FederatedCredentialName' already exists but does not match expected values.
Expected issuer: $expectedIssuer
Expected subject: $expectedSubject
Expected audience: $expectedAudience
Refusing to overwrite existing federated credential silently.
"@
    }

    Write-Host "Federated credential already exists and matches expected issuer/subject/audience." -ForegroundColor Green
} else {
    Write-Host "Federated credential not found. Creating..." -ForegroundColor Yellow

    $tempFile = [System.IO.Path]::GetTempFileName()
    try {
        $fcDefinition = @{
            name        = $FederatedCredentialName
            issuer      = $expectedIssuer
            subject     = $expectedSubject
            description = "OIDC trust for GitHub Actions $GitHubEnvironment environment"
            audiences   = @($expectedAudience)
        }

        $fcDefinition | ConvertTo-Json -Depth 5 | Set-Content -Path $tempFile -Encoding utf8
        $null = Get-AzCliValue "az ad app federated-credential create --id `"$appId`" --parameters @$tempFile"
    }
    finally {
        if (Test-Path $tempFile) {
            Remove-Item -Path $tempFile -Force
        }
    }

    Write-Host "Federated credential created." -ForegroundColor Green
}

Write-Step "Ensuring RBAC assignment exists"
$rbacExists = Get-AzCliValue "az role assignment list --assignee-object-id `"$spObjectId`" --scope `"$webAppScope`" --query `[?roleDefinitionName=='$RoleName'] | length(@)` -o tsv"
if ($rbacExists -eq '0') {
    Write-Host "Role assignment missing. Creating '$RoleName' on Web App scope..." -ForegroundColor Yellow
    $null = Get-AzCliValue "az role assignment create --assignee-object-id `"$spObjectId`" --role `"$RoleName`" --scope `"$webAppScope`" --query id -o tsv"
    Write-Host "Role assignment created." -ForegroundColor Green
} else {
    Write-Host "Role assignment already exists." -ForegroundColor Green
}

Write-Step "Manual GitHub Environment configuration required"
Write-Host "Configure the following in GitHub Environment '$GitHubEnvironment' manually:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Environment secrets:"
Write-Host "  AZURE_CLIENT_ID = <appId>"
Write-Host "  AZURE_TENANT_ID = <tenantId>"
Write-Host "  AZURE_SUBSCRIPTION_ID = <subscriptionId>"
Write-Host ""
Write-Host "Environment variables:"
Write-Host "  AZURE_WEBAPP_NAME = app-sharesafely-dev-we-01"
Write-Host "  DOTNET_VERSION = 8.0.x"
Write-Host "  PROJECT_PATH = src/ShareSafely.Web/ShareSafely.Web.csproj"
Write-Host ""
Write-Host "Resolved values (for manual setup):"
Write-Host "  AZURE_CLIENT_ID = $appId"
Write-Host "  AZURE_TENANT_ID = $tenantId"
Write-Host "  AZURE_SUBSCRIPTION_ID = $subscriptionId"
Write-Host ""
Write-Host "Reminder: GitHub Environment '$GitHubEnvironment', secrets, and variables are NOT automated by this script." -ForegroundColor Yellow
