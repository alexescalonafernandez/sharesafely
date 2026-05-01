# App Service Deployment Baseline (B4.0)

## 1) Purpose
This document records the **B4.0 operational baseline** for running ShareSafely on Azure App Service. It captures the current deployment topology, runtime configuration, manual deployment process, and validation outcomes for development/testing use.

## 2) Azure resources used
- **Subscription resource group**: `rg-sharesafely-dev-we-01`
- **Region**: `West Europe`
- **App Service Plan**: `asp-sharesafely-dev-we-01`
  - SKU: `Basic B1`
  - OS: `Linux`
- **Web App**: `app-sharesafely-dev-we-01`
  - Runtime: `.NET 8`
  - Public URL: `https://app-sharesafely-dev-we-01.azurewebsites.net`
- **Storage Account**: `stsharesafelydevwe01`
- **Blob Container**: `uploads` (private access)

## 3) Managed Identity and RBAC
- Web App uses a **system-assigned Managed Identity**.
- Managed Identity principalId: `837fcb80-80bd-450a-9426-9688c8c97d06`
- RBAC assignment in place:
  - Role: `Storage Blob Data Contributor`
  - Scope: storage account `stsharesafelydevwe01`
- Access pattern:
  - Application authenticates to Azure Storage via Azure Identity (`DefaultAzureCredential`) and does not require storage account keys in app configuration.

## 4) App Service application settings
Configured application settings:

- `Storage__Provider = AzureBlob`
- `AzureStorage__AccountName = stsharesafelydevwe01`
- `AzureStorage__BlobContainerName = uploads`
- `ShareLinks__ExpirationMinutes = 15`
- `Upload__MaxFileSizeBytes = 5242880`
- `Upload__AllowedExtensions__0 = .pdf`
- `Upload__AllowedExtensions__1 = .txt`
- `Upload__AllowedExtensions__2 = .png`
- `Upload__AllowedExtensions__3 = .jpg`
- `Upload__AllowedExtensions__4 = .jpeg`
- `Upload__LocalStoragePath = App_Data/uploads`

Notes:
- No connection strings, storage keys, or account keys are required for the current Azure Blob path.
- Blob container remains private; sharing is done through time-limited SAS links.

## 5) Manual deployment method
Current deployment method is **manual**:
1. Publish app locally with `dotnet publish`.
2. Package published output to a zip archive.
3. Deploy zip package to Azure App Service via Azure CLI (zip deploy).

This is the current B4.0 baseline and is intentionally simple for initial operational rollout.

## 6) Validation performed
Manual checks completed:
- App URL loads successfully.
- Uploading an image from the deployed app succeeds.
- Uploaded file is stored in Azure Blob Storage container `uploads`.
- App generates a SAS share link.
- Opening the link in another browser tab works.
- SAS link expires after 15 minutes as configured.

## 7) Troubleshooting notes
If deployment or runtime behavior fails, verify in this order:
1. **App settings**
   - Ensure `Storage__Provider` is `AzureBlob`.
   - Confirm storage account/container names are correct.
2. **Managed Identity status**
   - Confirm system-assigned identity is enabled on the Web App.
3. **RBAC scope/role**
   - Confirm `Storage Blob Data Contributor` is assigned to the Web App identity at the storage account scope.
4. **Container configuration**
   - Confirm container `uploads` exists and is private.
5. **Runtime/hosting**
   - Confirm App Service runtime is `.NET 8` on Linux.
6. **Link behavior**
   - If share links fail, validate that token expiration window (`ShareLinks__ExpirationMinutes`) matches expectations.

## 8) Cost and cleanup notes
- `Basic B1` was selected for **development/testing**.
- To control cost when not actively using the environment, stop, scale down, or delete non-required resources (especially App Service compute).
- Periodically remove test blobs from `uploads` to keep storage costs low.

## 9) What is not implemented yet
The following are **not** part of this B4.0 baseline:
- Automated CI/CD pipelines (for example, GitHub Actions)
- Infrastructure as Code (for example, Bicep)
- Azure Key Vault integration for application secrets
- Centralized monitoring/alerting baseline

## 10) Next milestone
Recommended next step after B4.0:
- Introduce repeatable deployment automation and environment provisioning, e.g. GitHub Actions and/or Bicep, while preserving Managed Identity + RBAC access patterns.
- Keep the current manual deployment process as fallback until automation is stable.
