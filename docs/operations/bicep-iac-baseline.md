# Bicep IaC Baseline (B5.0)

## 1. Purpose
This document describes the completed B5.0 milestone where ShareSafely infrastructure is managed with Bicep at resource group scope. It captures the current baseline, deployment workflow, and operational checks.

## 2. IaC structure
Bicep files are located under:

- `infra/bicep/`
- Main template: [`infra/bicep/main.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/main.bicep)
- Development parameters: [`infra/bicep/dev.bicepparam`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/dev.bicepparam)

Notes:
- `.bicepparam` is used (not JSON parameter files).
- Local helper scripts under `.azcli/` are intentionally ignored by Git.

## 3. Deployment scope
- Scope: **resource group** deployment.
- Target resource group: `rg-sharesafely-dev-we-01`.
- Current limitation: the **resource group itself is not created by Bicep yet**. The template targets an already existing resource group.

## 4. Resources managed by Bicep
The baseline now defines and manages:

- Storage Account: `stsharesafelydevwe01`
  - `Standard_LRS`
  - `StorageV2`
  - HTTPS only
  - Minimum TLS 1.2
  - Public blob access disabled
  - Hot access tier
- Blob container: `uploads`
  - `publicAccess: None`
  - `defaultEncryptionScope: $account-encryption-key`
  - `denyEncryptionScopeOverride: false`
- App Service Plan: `asp-sharesafely-dev-we-01`
- Web App: `app-sharesafely-dev-we-01`
  - Linux runtime: `DOTNETCORE|8.0`
  - `alwaysOn: true`
  - `httpsOnly: true`
- System-assigned Managed Identity on Web App
- Web App application settings
- RBAC assignment:
  - Principal: Web App managed identity
  - Role: Storage Blob Data Contributor
  - Scope: Storage Account

## 5. Parameters and environment file
Environment-specific values are supplied through [`infra/bicep/dev.bicepparam`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/dev.bicepparam).

App settings are managed by Bicep and include:
- `Storage__Provider`
- `AzureStorage__AccountName`
- `AzureStorage__BlobContainerName`
- `ShareLinks__ExpirationMinutes`
- `Upload__MaxFileSizeBytes`
- `Upload__AllowedExtensions__0` through `Upload__AllowedExtensions__4`
- `Upload__LocalStoragePath`

Operational guidance:
- Keep secrets out of Bicep parameters and source control.
- Continue using managed identity and RBAC for storage access.

## 6. Build, what-if, and deployment commands
Run from repository root.

```bash
# Validate template syntax/expansion without emitting ARM files
az bicep build --file infra/bicep/main.bicep --stdout

# Preview changes
az deployment group what-if \
  --resource-group rg-sharesafely-dev-we-01 \
  --parameters infra/bicep/dev.bicepparam

# Apply changes
az deployment group create \
  --resource-group rg-sharesafely-dev-we-01 \
  --parameters infra/bicep/dev.bicepparam

# Re-run what-if after deployment (expect no drift)
az deployment group what-if \
  --resource-group rg-sharesafely-dev-we-01 \
  --parameters infra/bicep/dev.bicepparam
```

## 7. RBAC migration note
The previous manual RBAC assignment was removed and recreated through Bicep. This avoids duplicate role assignments and makes authorization state declarative.

Implementation note:
- Role assignment uses a deterministic GUID.
- Role assignment API version is `2022-04-01`.

## 8. Validation performed
The following checks were completed for B5.0:

- Bicep build validation (`az bicep build --stdout`).
- Pre-deployment preview (`az deployment group what-if`).
- Deployment apply (`az deployment group create`).
- Post-deployment drift check (`what-if` rerun).
- Public app smoke test after app settings moved to Bicep.
- Functional checks remained healthy:
  - Upload flow
  - Azure Blob write/read path
  - User Delegation SAS link generation
  - SAS link access

## 9. Troubleshooting notes
- If deployment fails on RBAC role assignment, check whether an equivalent manual assignment still exists and remove duplicates before rerun.
- If app cannot access blob storage, verify:
  - Web App managed identity is enabled.
  - Storage Blob Data Contributor is assigned at storage account scope.
  - App settings values match the deployed storage account/container.
- If `what-if` shows unexpected churn in app settings, confirm no manual portal edits have diverged from Bicep-managed values.
- If `az deployment group` fails, confirm deployment target is the intended existing resource group (`rg-sharesafely-dev-we-01`).

## 10. What is not implemented yet
Not part of the current B5.0 baseline:

- Resource group creation/management by Bicep
- GitHub Actions or other CI/CD pipeline deployment automation
- Azure Key Vault integration
- Centralized monitoring, diagnostics consolidation, or alerting baseline

## 11. Next milestone
Recommended focus for the next milestone:

1. Add automated deployment pipeline (build + what-if + gated apply).
2. Expand environment support beyond dev (for example test/prod parameter files).
3. Introduce secret/config hardening strategy (for example Key Vault references) once pipeline and environment separation are in place.
4. Add monitoring and alert primitives after baseline deployment automation is stable.
