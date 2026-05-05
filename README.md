# ShareSafely

## 1) Project overview
ShareSafely is a .NET 8 ASP.NET Core Razor Pages application built as an Azure cloud engineering portfolio project. It demonstrates secure file sharing and an end-to-end Azure delivery workflow using modern identity-first and infrastructure-as-code practices.

The project is inspired by the ShareSafely practice brief from `cloud-engineering-projects/az-104/sharesafely.md`, implemented here with a security model that avoids storage account keys and connection strings for Blob access.

## 2) Live demo
ShareSafely is currently deployed to Azure App Service:

https://app-sharesafely-dev-we-01.azurewebsites.net

> This is a development/demo deployment. Availability and retained uploaded files are not guaranteed.  
> Do not upload sensitive, confidential, or personal information.

The demo environment may be torn down and rebuilt for cost-control and portfolio operations. Azure infrastructure is defined with Bicep and can be recreated when needed, while selected bootstrap steps (for example GitHub OIDC federation and GitHub environment configuration) are tracked separately.

## 3) What ShareSafely does
- Accepts file uploads through a Razor Pages web UI.
- Validates uploads.
- Supports local upload provider flow for development.
- Uploads files to a private Azure Blob Storage container (`uploads`) in Azure mode.
- Generates safe blob names using GUID + original file extension.
- Preserves content type during Blob upload.
- Returns read-only, time-limited User Delegation SAS links for sharing.

## 4) Architecture overview
- **Web app:** ASP.NET Core Razor Pages on .NET 8.
- **Storage:** Azure Blob Storage with private container access.
- **Identity:** System-assigned Managed Identity on Azure App Service.
- **Authorization:** RBAC assignment (`Storage Blob Data Contributor`) for the web app identity on the Storage Account.
- **Link sharing:** User Delegation SAS (read-only, time-limited).
- **Infrastructure:** Modular Bicep templates and parameter files.
- **Delivery:** GitHub Actions using OIDC to authenticate to Azure.
- **Observability:** Application Insights baseline and KQL validation queries.
- **Lifecycle management:** Blob lifecycle rule deletes old uploads in dev.

## 5) Azure services used
- Azure App Service
- Azure Blob Storage
- Azure Storage Lifecycle Management
- Azure Managed Identity
- Azure RBAC
- Azure Application Insights
- Azure Monitor / Application Insights Logs
- Azure Resource Manager / Bicep

## 6) Security model
- Blob container access is private by default.
- Files are not publicly exposed.
- Access is granted via read-only, time-limited User Delegation SAS links.
- The app uses Managed Identity + RBAC for Azure Storage access instead of storage account keys.
- GitHub Actions authenticates to Azure via OIDC (no publish profile and no client secret).
- Key Vault is **not currently implemented** for Blob credentials because the current design avoids storing Blob Storage credentials.

## 7) Infrastructure as Code
Bicep is organized in a modular structure:

- `infra/bicep/main.bicep` (orchestrator)
- `infra/bicep/dev.bicepparam` (dev environment values)
- `infra/bicep/modules/storage.bicep`
- `infra/bicep/modules/monitoring.bicep`
- `infra/bicep/modules/webapp.bicep`
- `infra/bicep/modules/rbac.bicep`

Notes:
- Application settings are managed through Bicep.
- Resource Group creation is **not currently handled** by Bicep.

## 8) CI/CD deployment
Deployment workflow:

- Workflow file: `.github/workflows/deploy-webapp.yml`
- Triggers: `push` to `main` and `workflow_dispatch`
- Uses GitHub environment: `dev`
- Authenticates with `azure/login` using OIDC
- Builds, tests, publishes, and deploys the web app to Azure App Service

This implementation does not use publish profiles, client secrets, or storage account keys for deployment/storage authentication.

## 9) Monitoring and cleanup
- Application Insights monitoring baseline is implemented.
- KQL queries are used for telemetry validation.
- Blob lifecycle management policy is implemented with:
  - Rule name: `delete-old-uploaded-blobs`
  - Scope: `blockBlob`
  - `prefixMatch`: `uploads/`
  - Behavior: deletes uploaded blobs after 7 days since last modification (dev baseline)

## 10) Local development
1. Install the .NET 8 SDK.
2. Restore packages:
   ```bash
   dotnet restore
   ```
3. Build:
   ```bash
   dotnet build
   ```
4. Run tests:
   ```bash
   dotnet test
   ```
5. Run the web app:
   ```bash
   dotnet run --project src/ShareSafely.Web
   ```

Local provider/local upload is supported for development workflows.

## 11) Repository structure
- `src/ShareSafely.Web` — ASP.NET Core Razor Pages application
- `tests` — test projects
- `infra/bicep` — Bicep IaC templates and parameters
- `docs/operations` — operational and delivery documentation
- `.github/workflows` — CI/CD workflows

## 12) Operational documentation
- [Bicep IaC baseline](docs/operations/bicep-iac-baseline.md)
- [GitHub Actions OIDC deployment](docs/operations/github-actions-oidc-deployment.md)
- [Monitoring baseline](docs/operations/monitoring-baseline.md)
- [Bicep modularization](docs/operations/bicep-modularization.md)
- [Blob cleanup lifecycle](docs/operations/blob-cleanup-lifecycle.md)

## 13) Completed milestones
- B1.0 Local upload workflow
- B2.0 Azure Blob Storage integration
- B3.0 SAS-based time-limited share links
- B4.0 Azure App Service deployment baseline
- B5.0 Bicep IaC baseline
- B6.0 GitHub Actions OIDC deployment
- B7.0 Monitoring baseline
- B8.0 Bicep modularization and IaC cleanup
- B9.0 Blob cleanup lifecycle baseline

## 14) Known limitations / future improvements
- OIDC bootstrap setup is documented/tracked separately and is not fully automated as IaC yet.
- Resource Group creation is not currently handled by Bicep.
- Key Vault is not implemented because current storage access avoids secrets; it may be introduced later if a real secret management need appears.
- No authentication/user accounts for upload page yet.
- No per-file expiration metadata.
- Blobs are not deleted immediately when SAS links expire.
- Current demo is dev/demo and may be unavailable.
- Future portfolio hosting/cost strategy is still to be defined.
