# Blob cleanup lifecycle (B9.0 baseline)

## 1. Purpose
Define and operationalize a baseline Azure Blob Storage cleanup lifecycle for ShareSafely development environments so uploaded files are retained for a limited period and then automatically removed.

## 2. Cleanup problem being solved
Before B9.0, uploaded blobs could accumulate indefinitely in storage unless manually removed. This increases storage usage and operational overhead over time. The B9.0 lifecycle policy introduces automatic cleanup for uploaded blobs.

## 3. SAS expiration vs blob lifecycle cleanup
These are separate controls:

- **SAS expiration** limits how long a generated share link can access a blob.
- **Lifecycle cleanup** governs how long the blob is physically retained in storage.

In ShareSafely, SAS links expire on a short time window, but the underlying blob remains until lifecycle management deletes it.

## 4. Lifecycle policy configuration
The deployed lifecycle management policy has the following baseline settings:

- **Resource type:** `Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01`
- **Policy name:** `default`
- **Rule name:** `delete-old-uploaded-blobs`
- **Rule state:** enabled
- **Scope:**
  - `blobTypes`: `blockBlob`
  - `prefixMatch`: `uploads/`
- **Action:** delete base blobs after `daysAfterModificationGreaterThan` days

For dev baseline, this is configured as **7 days since last modification**.

## 5. Bicep implementation
The lifecycle management policy is defined in:

- `infra/bicep/modules/storage.bicep`

The rule targets uploaded blobs in the private `uploads` container path (`uploads/`).

## 6. Parameterization
The cleanup threshold is parameterized via:

- `deleteUploadedBlobsAfterDays`

Current dev value:

- `infra/bicep/dev.bicepparam`: `deleteUploadedBlobsAfterDays = 7`

This enables environment-specific retention tuning without changing module logic.

## 7. Validation performed
The following deployment and verification steps were completed:

- `az bicep build` completed successfully.
- `az deployment group what-if` showed only creation of the lifecycle management policy.
- No unexpected delete/modify operations were observed for Storage Account, container, Web App, RBAC, or monitoring resources.
- `az deployment group create` completed successfully.
- Azure Portal verification confirmed the policy under Storage Account lifecycle management.
- Policy state is active.

## 8. Operational notes
- Uploaded blobs are stored in a private container named `uploads`.
- Blob names are generated as `GUID + original extension`.
- Cleanup applies uniformly to all uploaded file types in `uploads/` (for example `.pdf`, `.txt`, `.png`, `.jpg`, `.jpeg`) because the policy does not filter by extension.
- Deletion timing is based on lifecycle processing and the configured age threshold, not SAS expiration.

## 9. What is not implemented yet
- No immediate blob deletion when SAS expires.
- No Azure Functions or Logic Apps based cleanup workflow.
- No per-file expiration metadata driven retention model.
- No extension-specific cleanup rules.

## 10. Next milestone
Introduce a production-ready retention strategy that keeps infrastructure-as-code parameterized per environment (for example, distinct dev/stage/prod retention windows) and documents governance decisions for retention duration and compliance alignment.
