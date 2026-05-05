# GitHub Actions OIDC bootstrap (manual)

## 1) Purpose
This document explains the **manual bootstrap** required so the ShareSafely GitHub Actions workflow can authenticate to Azure using OpenID Connect (OIDC) and deploy to the development Web App.

It exists to make the current setup reproducible and transparent while automation work is tracked in [Issue #8](https://github.com/alexescalonafernandez/sharesafely/issues/8).

## 2) What is already versioned
The deployment pipeline is already committed in:

- [`.github/workflows/deploy-webapp.yml`](https://github.com/alexescalonafernandez/sharesafely/blob/main/.github/workflows/deploy-webapp.yml)

That workflow already defines:

- GitHub Actions permissions: `id-token: write`, `contents: read`
- Job environment: `dev`
- Azure login via `azure/login` using OIDC
- Build, test, publish, and deploy to Azure App Service

## 3) What is manual bootstrap
The following setup is currently manual and must be done outside source control:

1. Microsoft Entra App Registration + Service Principal
2. Federated credential for GitHub Actions OIDC
3. Azure RBAC assignment for pipeline identity
4. GitHub Environment (`dev`)
5. GitHub Environment secrets and variables

## 4) Azure identity setup
Use Azure CLI and placeholders where needed.

```bash
# ----- Required values -----
resourceGroupName="rg-sharesafely-dev-we-01"
webAppName="app-sharesafely-dev-we-01"
githubOrgOrUser="alexescalonafernandez"
githubRepo="sharesafely"
appRegistrationName="sp-sharesafely-github-actions-dev"

# ----- Discover tenant/subscription -----
subscriptionId="$(az account show --query id -o tsv)"
tenantId="$(az account show --query tenantId -o tsv)"

# ----- Scope deployment identity only to the Web App resource -----
webAppScope="/subscriptions/${subscriptionId}/resourceGroups/${resourceGroupName}/providers/Microsoft.Web/sites/${webAppName}"

# ----- Create App Registration -----
appId="$(az ad app create --display-name "${appRegistrationName}" --query appId -o tsv)"

# ----- Create Service Principal -----
az ad sp create --id "${appId}"
```

## 5) Federated credential setup
Because the workflow job uses `environment: dev`, the correct federated credential subject is:

- `repo:alexescalonafernandez/sharesafely:environment:dev`

Create a local JSON file (example name: `federated-credential.json`):

```json
{
  "name": "github-oidc-dev",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:alexescalonafernandez/sharesafely:environment:dev",
  "description": "OIDC trust for GitHub Actions dev environment",
  "audiences": [
    "api://AzureADTokenExchange"
  ]
}
```

Apply it:

```bash
az ad app federated-credential create \
  --id "${appId}" \
  --parameters @federated-credential.json
```

## 6) GitHub Environment setup
Create the environment in GitHub:

1. Go to **Settings → Environments** in `alexescalonafernandez/sharesafely`
2. Select **New environment**
3. Name it `dev`

## 7) Required secrets and variables
Configure these in the `dev` GitHub Environment.

### Environment secrets
- `AZURE_CLIENT_ID` = `<appId from az ad app create>`
- `AZURE_TENANT_ID` = `<tenantId>`
- `AZURE_SUBSCRIPTION_ID` = `<subscriptionId>`

### Environment variables
- `AZURE_WEBAPP_NAME` = `app-sharesafely-dev-we-01`
- `DOTNET_VERSION` = `8.0.x`
- `PROJECT_PATH` = `src/ShareSafely.Web/ShareSafely.Web.csproj`

## 8) RBAC scope and least privilege note
Assign deployment permissions to the pipeline identity with **Contributor** role on the **Web App resource scope only** (not entire subscription).

```bash
# Resolve service principal object id from appId
spObjectId="$(az ad sp show --id "${appId}" --query id -o tsv)"

# Assign Contributor on Web App only
az role assignment create \
  --assignee "${spObjectId}" \
  --role "Contributor" \
  --scope "${webAppScope}"

# Verify assignment
az role assignment list \
  --assignee "${spObjectId}" \
  --scope "${webAppScope}" \
  -o table
```

## 9) Common OIDC subject mismatch error
### Symptom
Azure login step fails with:

- `AADSTS700213: No matching federated identity record found for presented assertion subject.`

### Root cause in this repository
An initial federated credential used a **branch-based** subject:

- `repo:alexescalonafernandez/sharesafely:ref:refs/heads/main`

But the workflow emits an **environment-based** subject because the job uses `environment: dev`:

- `repo:alexescalonafernandez/sharesafely:environment:dev`

### Fix
Create/update federated credential so `subject` is exactly:

- `repo:alexescalonafernandez/sharesafely:environment:dev`

## 10) Validation checklist
After bootstrap, validate:

- [ ] App Registration exists for `sp-sharesafely-github-actions-dev`
- [ ] Service Principal exists for that app
- [ ] Federated credential exists with subject `repo:alexescalonafernandez/sharesafely:environment:dev`
- [ ] Contributor role assignment exists at Web App scope:
      `/subscriptions/<SUBSCRIPTION_ID>/resourceGroups/rg-sharesafely-dev-we-01/providers/Microsoft.Web/sites/app-sharesafely-dev-we-01`
- [ ] GitHub Environment `dev` exists
- [ ] `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` secrets are set in `dev`
- [ ] `AZURE_WEBAPP_NAME`, `DOTNET_VERSION`, `PROJECT_PATH` variables are set in `dev`
- [ ] A workflow run from `main` completes Azure login and deploy steps

## 11) Security notes
- Do **not** use publish profiles for this pipeline.
- Do **not** use client secrets for this pipeline identity.
- Do **not** store Azure credentials in source control.
- OIDC uses short-lived tokens issued at runtime.
- Keep RBAC scoped to the Web App resource for deployment needs.

## 12) What is not automated yet
- OIDC bootstrap is **not** fully automated as Infrastructure as Code yet.
- GitHub Environment creation is manual.
- GitHub Environment secrets and variables are manual.
- GitHub secrets/variables are **not** managed by Bicep.

## 13) Future automation options
Potential follow-up options:

1. Script Azure bootstrap with Azure CLI/PowerShell (idempotent, documented inputs/outputs).
2. Add validation scripts to verify federated credential subject and RBAC scope.
3. Add repository onboarding runbook/checklist for new environments (dev/test/prod).
4. Evaluate GitHub API/CLI automation for environment variable/secrets provisioning (while keeping secret values outside source control).

## 14) Related issue
- [Issue #8](https://github.com/alexescalonafernandez/sharesafely/issues/8) - Track OIDC bootstrap automation and documentation
