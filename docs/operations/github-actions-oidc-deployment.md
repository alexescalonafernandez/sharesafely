# GitHub Actions OIDC Deployment Baseline (B6.0)

## 1. Purpose
This document records the **B6.0 operational baseline** for deploying ShareSafely to Azure App Service using **GitHub Actions + Azure OIDC federated identity**. It captures the current workflow, required environment configuration, manual bootstrap steps, and practical troubleshooting guidance.

## 2. Workflow overview
Workflow file: `.github/workflows/deploy-webapp.yml`  
Workflow name: `Deploy ShareSafely Web App`

Triggers:
- `push` to `main`
- `workflow_dispatch`

Job highlights:
- Uses GitHub `environment: dev`
- Permissions:
  - `id-token: write`
  - `contents: read`
- Steps:
  1. Checkout
  2. Setup .NET
  3. `dotnet restore`
  4. `dotnet build`
  5. `dotnet test`
  6. `dotnet publish`
  7. `azure/login` using OIDC
  8. `azure/webapps-deploy` to Azure App Service

## 3. GitHub environment configuration
Environment used by the workflow:
- Name: `dev`

Environment secrets:
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Environment variables:
- `AZURE_WEBAPP_NAME`
- `DOTNET_VERSION`
- `PROJECT_PATH`

> Note: GitHub Environment `dev` was configured manually as a bootstrap step.

## 4. Azure OIDC identity setup
B6.0 uses an Azure App Registration / Service Principal for GitHub Actions authentication via OIDC.

Manual bootstrap that was completed:
- Created App Registration / Service Principal for CI/CD deployment.
- Added a federated credential that matches the workflow subject for environment-based deployments.
- Stored Service Principal identifiers in GitHub Environment `dev` secrets.

## 5. Federated credential subject gotcha
A key setup detail is that the OIDC token subject must match the workflow execution context.

- Initial (failing) subject expected by Azure federated credential:
  - `repo:alexescalonafernandez/sharesafely:ref:refs/heads/main`
- Actual subject emitted by the workflow (because it uses `environment: dev`):
  - `repo:alexescalonafernandez/sharesafely:environment:dev`

The mismatch caused Azure login failures. Configuring the federated credential with the **environment-based subject** fixed the issue.

## 6. Azure RBAC for deployment
Deployment identity scope is intentionally limited:
- Principal: GitHub Actions Service Principal
- Role: `Contributor`
- Scope: Web App resource only (`app-sharesafely-dev-we-01`)

Target resources:
- Web App: `app-sharesafely-dev-we-01`
- Resource Group: `rg-sharesafely-dev-we-01`
- Public URL: `https://app-sharesafely-dev-we-01.azurewebsites.net`

## 7. Deployment flow
1. A push to `main` (or manual dispatch) starts the workflow.
2. The app is restored, built, tested, and published.
3. GitHub requests an OIDC token (`id-token: write`).
4. `azure/login` exchanges that token for Azure access using the federated credential.
5. `azure/webapps-deploy` deploys the published app to `app-sharesafely-dev-we-01`.

## 8. Validation performed
Validated in B6.0:
- Workflow run completed successfully in GitHub Actions.
- Azure login through OIDC succeeded.
- Build, test, publish, and deploy steps succeeded.
- Deployed app was manually validated after deployment:
  - Upload works
  - Azure Blob storage integration works
  - SAS link generation works
  - SAS link access works

## 9. Security notes
This baseline avoids long-lived deployment credentials:
- No publish profiles
- No client secrets
- No connection strings for deployment auth
- No storage account keys / account keys

Authentication model:
- Short-lived OIDC token exchange from GitHub to Azure
- Federated trust bound to repository + environment subject
- RBAC scope limited to the deployment target

## 10. Troubleshooting notes
If deployment fails, check in this order:
1. **GitHub Environment binding**
   - Workflow job uses `environment: dev`.
2. **Federated credential subject**
   - Must match `repo:alexescalonafernandez/sharesafely:environment:dev`.
3. **Environment secrets/variables**
   - Verify `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and required variables are present in `dev`.
4. **RBAC**
   - Confirm Service Principal has `Contributor` on `app-sharesafely-dev-we-01` scope.
5. **Workflow logs**
   - Check `azure/login` first, then deploy action output.

## 11. What is not implemented yet
Not part of this B6.0 baseline:
- OIDC bootstrap fully automated as IaC
- Bicep deployment execution inside this GitHub Actions workflow
- Azure Key Vault integration
- Monitoring and alerting baseline

## 12. Related backlog
- Issue #8: **Track OIDC bootstrap automation and documentation**

This issue tracks future improvements for documenting and automating today’s manual OIDC bootstrap process.

## 13. Next milestone
Recommended next step after B6.0:
- Automate/document the OIDC bootstrap path (Issue #8), then extend deployment automation toward end-to-end infra + app delivery while preserving OIDC + least-privilege RBAC.
