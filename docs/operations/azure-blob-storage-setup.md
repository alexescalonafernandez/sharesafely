# Azure Blob Storage setup (B2.0)

## 1. Purpose
This document describes the operational setup for milestone **B2.0**, where ShareSafely stores uploaded files in Azure Blob Storage instead of local disk.

## 2. Azure resources created
- **Resource Group:** `rg-sharesafely-dev-we-01`
- **Region:** `West Europe`
- **Storage Account:** `stsharesafelydevwe01`
- **Blob Container:** `uploads`
- **Container access level:** `Private` (no anonymous public access)

## 3. Authentication and permissions
- Authentication uses **Azure Identity** with `DefaultAzureCredential`.
- Local developers authenticate with Azure CLI / developer identity (no connection string required).
- Required role assignment for local development:
  - **Storage Blob Data Contributor** on storage account `stsharesafelydevwe01`.

## 4. Application configuration
Set provider selection and Azure storage options in app configuration:

```json
"Storage": {
  "Provider": "AzureBlob"
},
"AzureStorage": {
  "AccountName": "stsharesafelydevwe01",
  "BlobContainerName": "uploads"
}
```

Implementation notes:
- `AzureBlobStorageService` implements `IFileStorageService`.
- `LocalFileStorageService` still exists for local-provider mode.
- `StorageOptions.Provider` controls which provider is active.
- Supported provider values: `Local`, `AzureBlob`.

## 5. How to run locally against Azure Blob Storage
1. Sign in to Azure locally:
   ```bash
   az login
   ```
2. Confirm your identity has **Storage Blob Data Contributor** on `stsharesafelydevwe01`.
3. Set app configuration to:
   - `Storage:Provider = AzureBlob`
   - `AzureStorage:AccountName = stsharesafelydevwe01`
   - `AzureStorage:BlobContainerName = uploads`
4. Start the app and upload a file from the Razor Pages UI.

## 6. How to verify uploaded blobs
### Azure CLI
List blobs in the container:

```bash
az storage blob list --account-name stsharesafelydevwe01 --container-name uploads --auth-mode login --output table
```

If needed, inspect details for a specific blob:

```bash
az storage blob show --account-name stsharesafelydevwe01 --container-name uploads --name <blob-name> --auth-mode login --output json
```

### Azure Portal
You can also verify manually in the Azure Portal by opening storage account `stsharesafelydevwe01` → **Containers** → `uploads` and checking that the uploaded file exists. A manual test for B2.0 confirmed upload, download, and open of an image.

## 7. Troubleshooting notes
- **Authentication errors (401/403):**
  - Re-run `az login`.
  - Verify your active tenant/subscription and RBAC assignment.
  - Confirm role scope includes storage account `stsharesafelydevwe01`.
- **Container not found:**
  - Confirm `AzureStorage:BlobContainerName` is exactly `uploads`.
- **Provider mismatch (files still local):**
  - Confirm `Storage:Provider` is set to `AzureBlob`.
- **General upload failures:**
  - Check application logs for Azure SDK exceptions and blob path/name details.

## 8. What is not implemented yet
The following are intentionally out of scope for B2.0:
- SAS-based share links
- App Service deployment integration
- Key Vault secret management
- Infrastructure-as-code (for example Bicep/Terraform)
- Centralized monitoring/alerts
- Automated lifecycle/cleanup policies

## 9. Next milestone
**B3.0** will introduce **SAS-based time-limited share links** for controlled external sharing.
