# SAS share links operations (B3.0)

## 1. Purpose
This document describes the operational behavior for milestone **B3.0**, where ShareSafely generates time-limited SAS share links after file upload.

## 2. Design decision: separate sharing from storage
B3.0 keeps responsibilities separated:
- `IFileStorageService` handles storing files.
- `IShareLinkService` handles generating share links.

This allows share-link strategy changes without changing upload storage logic. In the current implementation, `AzureBlobSasShareLinkService` implements `IShareLinkService`.

## 3. User Delegation SAS approach
The implementation generates **User Delegation SAS** links for individual blobs.

Key characteristics:
- Uses `DefaultAzureCredential`.
- Does **not** use storage account keys.
- Does **not** use connection strings.
- Generates **read-only** links scoped to a single blob.
- Access expires automatically when the SAS expiry time is reached.

## 4. Application configuration
Share link expiration is configured with:

```json
"ShareLinks": {
  "ExpirationMinutes": 15
}
```

Operational notes:
- Standard B3.0 value is 15 minutes.
- During manual validation, `ExpirationMinutes` was temporarily set to `1` minute to confirm expiry behavior.

## 5. Runtime flow
1. User uploads a file through the Razor Pages UI.
2. The file is stored in Azure Blob Storage (container `uploads`, private access).
3. After successful storage, the app requests a share link from `IShareLinkService`.
4. `AzureBlobSasShareLinkService` generates a read-only User Delegation SAS URL for that blob.
5. The generated link is returned to the UI for sharing.

## 6. Manual validation performed
Manual B3.0 validation (local development) confirmed:
- SAS link is generated after successful upload.
- Link works before expiration.
- Link stops working after expiration.
- Image files render inline when opened via share link because uploaded blob `Content-Type` is preserved.

Validation context:
- Tested locally using Azure CLI login / developer identity.
- Azure resources used:
  - Resource Group: `rg-sharesafely-dev-we-01`
  - Storage Account: `stsharesafelydevwe01`
  - Container: `uploads` (private)
- Required local role: `Storage Blob Data Contributor` on storage account `stsharesafelydevwe01`.

## 7. Security notes
- Private container access is retained; blob access is granted through time-limited SAS only.
- Read-only SAS reduces risk versus broader permissions.
- User Delegation SAS avoids long-lived account keys in application configuration.
- Keep SAS expiration short enough for the sharing scenario.
- For production deployment, use **App Service Managed Identity** with equivalent RBAC permissions instead of developer identity.

## 8. Troubleshooting notes
- **No share link generated:**
  - Confirm upload succeeded first.
  - Verify `Storage:Provider` and Azure storage settings are correct.
  - Check logs for `IShareLinkService` / Azure SDK exceptions.
- **401/403 when opening link before expected expiry:**
  - Confirm system clock consistency.
  - Re-check local Azure login (`az login`) and RBAC assignment.
  - Validate blob and container names match configuration.
- **Link still works longer than expected:**
  - Confirm actual `ShareLinks:ExpirationMinutes` value at runtime.
  - Re-test with a short value (for example `1`) and a newly generated link.
- **Image does not render inline:**
  - Confirm the uploaded blob `Content-Type` is correct.

## 9. What is not implemented yet
Out of scope for B3.0:
- User authentication/authorization for who can request share links.
- App Service production wiring.
- Key Vault integration.
- Infrastructure-as-code (for example Bicep/Terraform).
- Centralized monitoring/alerting.
- Automated cleanup/lifecycle governance for shared artifacts.

## 10. Next milestone
Next milestone should focus on production hardening of share-link operations, especially runtime identity, operational governance, and observability around link generation and access patterns.
