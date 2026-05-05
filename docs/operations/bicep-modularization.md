# B8.0 Operations Note: Bicep Modularization and IaC Cleanup

## 1. Purpose
B8.0 completed an Infrastructure-as-Code (IaC) refactor of the existing Azure Bicep deployment files. This milestone focused on maintainability and clarity by modularizing infrastructure definitions. It was **not** a functional deployment feature milestone and introduced no new infrastructure behavior.

## 2. Before and after structure
**Before (pre-B8.0):**
- [`infra/bicep/main.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/main.bicep) contained most resource definitions in one file.

**After (B8.0):**
- [`infra/bicep/main.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/main.bicep)
- [`infra/bicep/dev.bicepparam`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/dev.bicepparam)
- [`infra/bicep/modules/storage.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/storage.bicep)
- [`infra/bicep/modules/monitoring.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/monitoring.bicep)
- [`infra/bicep/modules/webapp.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/webapp.bicep)
- [`infra/bicep/modules/rbac.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/rbac.bicep)

The structure change is an internal refactor to improve separation of concerns.

## 3. Module responsibilities
- **`storage.bicep`**
  - Defines Storage Account.
  - Defines `uploads` blob container.
  - Outputs: `storageAccountId`, `storageAccountName`, `blobContainerName`.

- **`monitoring.bicep`**
  - Defines Application Insights.
  - Outputs Application Insights resource id and connection string.

- **`webapp.bicep`**
  - Defines App Service Plan.
  - Defines Web App.
  - Defines system-assigned Managed Identity.
  - Preserves Application Insights hidden-link tag.
  - Defines Web App app settings.
  - Outputs: `webAppName`, `webAppPrincipalId`.

- **`rbac.bicep`**
  - Defines Storage Blob Data Contributor role assignment.
  - Uses Storage Account as typed RBAC scope.
  - Uses deterministic GUID naming for the role assignment.
  - Uses direct parameters for `storageAccountName` and `webAppName` where start-time evaluation is required.
  - Uses `webAppPrincipalId` only in `properties.principalId`.

## 4. `main.bicep` as orchestrator
`main.bicep` remains at `targetScope = 'resourceGroup'` and now acts as the orchestrator:
- Defines environment-level parameters.
- Calls each module.
- Passes values between modules.
- Avoids directly defining the main Azure resources.

This keeps top-level deployment flow readable while moving resource details into focused modules.

## 5. Bicep start-time evaluation lessons
A key lesson in modularization is that not every value can be sourced from module outputs in every property.

Some Bicep properties (notably parts of RBAC role assignment definitions such as `name` and `scope`) must be resolvable at deployment start. If those fields depend on values that are not start-time resolvable, deployments can fail with `BCP120`.

Practical takeaway:
- Use module outputs for standard parameter flow.
- For start-time-constrained fields, use direct parameters and/or `existing` resource references that are start-time calculable.

## 6. RBAC modularization notes
RBAC extraction required special handling to preserve valid evaluation order:
- Role assignment naming uses deterministic GUID input that is start-time safe.
- Typed scope is based on Storage Account resource context.
- `storageAccountName` and `webAppName` are passed directly for start-time-sensitive construction.
- Managed identity principal id is still passed and used only where runtime-evaluated properties allow it (`properties.principalId`).

This pattern kept RBAC modular while avoiding start-time evaluation pitfalls.

## 7. Application Insights hidden-link note
The Web App module preserves the Application Insights hidden-link tagging convention. This ensures the existing relationship/association behavior between Web App and Application Insights remains intact after refactor.

## 8. Validation performed
Validation was done incrementally and at completion:
- `az bicep build` used after each module extraction to verify compile correctness.
- `az deployment group what-if` used after each extraction and at final state.
- Final `what-if` validation succeeded.

The `what-if` process was specifically used to check for unintended **Create/Delete/Modify** operations and confirm refactor safety.

## 9. What did not change
B8.0 intentionally did **not** change:
- App runtime functionality.
- Monitoring feature scope.
- CI/CD behavior.
- Intended deployed resource set.
- Resource Group creation approach (still not newly handled by this refactor).

## 10. What is not implemented yet
This milestone did not add new infrastructure features beyond modularization. Any future enhancements (for example additional services, environment expansion patterns, or policy hardening) remain out of scope for B8.0.

## 11. Next milestone
The next milestone should build on this cleaner module boundary to introduce **new functional IaC capabilities** (if needed), with the same validation discipline:
1. implement feature change,
2. run `az bicep build`,
3. run `az deployment group what-if`,
4. verify only expected diffs,
5. deploy.

B8.0 provides the maintainable structure to do this with lower risk.
