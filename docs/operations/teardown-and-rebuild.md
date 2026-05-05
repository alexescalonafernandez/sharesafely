# ShareSafely Teardown and Rebuild Strategy (B12.0)

## 1. Purpose

This document explains how to operate the ShareSafely dev/demo environment with practical cost control in mind, including teardown and rebuild decisions.

It is intentionally scoped to a dev/demo deployment and does **not** claim production readiness or full automation.

## 2. Current dev/demo resources

Current Azure resources for the ShareSafely dev/demo environment:

- Resource Group: `rg-sharesafely-dev-we-01`
- App Service: `app-sharesafely-dev-we-01`
- App Service Plan: `asp-sharesafely-dev-we-01`
- Storage Account: `stsharesafelydevwe01`
- Blob container: `uploads`
- Application Insights: `appi-sharesafely-dev-we-01`

Infrastructure-as-Code and deployment references:

- Bicep entry point: [`infra/bicep/main.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/main.bicep)
- Bicep parameter file: [`infra/bicep/dev.bicepparam`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/dev.bicepparam)
- GitHub Actions workflow: [`/.github/workflows/deploy-webapp.yml`](https://github.com/alexescalonafernandez/sharesafely/blob/main/.github/workflows/deploy-webapp.yml)
- OIDC bootstrap documentation: [`docs/operations/oidc-bootstrap.md`](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/oidc-bootstrap.md)

## 3. Cost considerations

Key cost concepts for this environment:

- The App Service Plan can generate cost while it exists, even with low traffic.
- Storage can generate cost from both data at rest and storage operations.
- Application Insights can generate cost from telemetry ingestion and retention.
- Blob lifecycle management helps reduce long-term storage accumulation, but does **not** make the environment free.

Because this is a live dev/demo deployment, it may be torn down when not needed to reduce spend.

## 4. Teardown options

### Option 1: Keep everything running

- **Pros:** Most convenient for continuous demo availability.
- **Cons:** Highest ongoing cost.
- **Use when:** You need immediate access at all times.

### Option 2: Scale down App Service Plan (if possible)

- **Pros:** Can reduce compute cost while keeping a public demo available.
- **Cons:** Feature limits and compatibility vary by tier and must be validated before relying on this approach.
- **Use when:** You want lower cost without full teardown.

### Option 3: Delete the Resource Group

- **Pros:** Strongest cost-control option.
- **Cons:** Removes Azure resources and data; demo is unavailable until rebuild.
- **Use when:** You can tolerate downtime and want to minimize ongoing cost.

### Option 4: Keep GitHub/OIDC bootstrap context but delete Azure app resources

- **Pros:** Useful if you rebuild often and want deployment prerequisites documented/retained outside Azure resources.
- **Cons:** Still requires clear handling of what remains in GitHub vs. what must be recreated in Azure.
- **Use when:** You are iterating on teardown/rebuild workflows.

## 5. What is preserved outside Azure

Deleting Azure resources does **not** delete repository-side assets. The following remain:

- GitHub repository content (including source, Bicep, and workflow files)
- GitHub Actions workflow definitions (for example [`/.github/workflows/deploy-webapp.yml`](https://github.com/alexescalonafernandez/sharesafely/blob/main/.github/workflows/deploy-webapp.yml))
- GitHub Environments and their configured secrets/variables (unless manually changed)
- OIDC bootstrap documentation in this repo

## 6. What is lost when deleting resources

If you delete the Resource Group (`rg-sharesafely-dev-we-01`), resources inside it are removed, including:

- App Service and App Service Plan
- Storage Account and blob data (including `uploads` contents)
- Application Insights and telemetry history
- Role assignments scoped to resources in that Resource Group

Additional impact notes:

- SAS links generated before teardown become invalid if underlying blobs/resources are deleted.
- Lifecycle cleanup may already remove older blobs over time; teardown is a full reset of hosted state.
- Demo availability is not guaranteed and depends on whether the environment is currently deployed.

## 7. Rebuild flow

> Important: Bicep in this project does **not** currently create the Resource Group. Create it manually first if missing.

1. Confirm Azure subscription and login context:

   ```bash
   az account show
   ```

2. Create Resource Group manually (if missing):

   ```bash
   az group create --name rg-sharesafely-dev-we-01 --location westeurope
   ```

3. Validate Bicep:

   ```bash
   az bicep build --file infra/bicep/main.bicep --stdout
   ```

4. Run what-if:

   ```bash
   az deployment group what-if \
     --resource-group rg-sharesafely-dev-we-01 \
     --template-file infra/bicep/main.bicep \
     --parameters infra/bicep/dev.bicepparam
   ```

5. Deploy infrastructure:

   ```bash
   az deployment group create \
     --resource-group rg-sharesafely-dev-we-01 \
     --template-file infra/bicep/main.bicep \
     --parameters infra/bicep/dev.bicepparam
   ```

6. Ensure OIDC bootstrap prerequisites exist (if not, follow [`docs/operations/oidc-bootstrap.md`](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/oidc-bootstrap.md)).

7. Run application deployment via GitHub Actions (`workflow_dispatch` or push to `main`).

8. Validate public app endpoint:

   - `https://app-sharesafely-dev-we-01.azurewebsites.net`

9. Validate end-to-end behavior:

   - Upload flow
   - SAS link access
   - Application Insights telemetry
   - Blob lifecycle policy behavior/presence

## 8. Post-rebuild validation checklist

Use this checklist after infrastructure and app deployment:

- [ ] Resource Group exists.
- [ ] Storage Account exists.
- [ ] `uploads` container exists and is private.
- [ ] Web App exists and is running.
- [ ] Managed Identity exists on Web App.
- [ ] Web App identity has `Storage Blob Data Contributor` on the Storage Account.
- [ ] Required App Settings exist.
- [ ] Application Insights exists.
- [ ] Lifecycle policy exists and is enabled.
- [ ] GitHub Actions can authenticate to Azure via OIDC.
- [ ] GitHub Actions can deploy successfully.
- [ ] App loads publicly.
- [ ] Upload works.
- [ ] SAS link works.
- [ ] KQL requests query returns telemetry.

## 9. Relationship with OIDC bootstrap

OIDC bootstrap and runtime app identity are separate concerns:

- **Pipeline identity (GitHub Actions):** Used for CI/CD authentication to Azure.
- **Web App managed identity:** Used by the running app for Azure resource access (for example Blob Storage via RBAC).

Important notes:

- OIDC bootstrap is documented, but not fully automated end-to-end.
- GitHub Environment secrets/variables are not managed by Bicep.
- If the Web App is recreated, its system-assigned Managed Identity principal ID may change.
- The Bicep RBAC module should recreate the Web App-to-Storage role assignment on deployment.

## 10. Portfolio hosting notes

This environment is for development/demo and portfolio demonstration. It is reasonable to keep it offline between demo windows to control cost.

A practical pattern is:

- Tear down when inactive for extended periods.
- Rebuild before demos.
- Keep deployment docs and pipeline prerequisites current so rebuilds stay predictable.

## 11. What is not automated yet

Current limitations to keep explicit:

- Resource Group creation is not currently handled by the Bicep deployment in this project.
- OIDC bootstrap is documented but not fully automated.
- GitHub Environment setup is not managed by Bicep.
- Full one-command teardown + rebuild automation is not implemented.

## 12. Future improvements

Potential next steps:

- Automate Resource Group creation if desired.
- Add teardown/rebuild scripts.
- Add idempotent OIDC bootstrap validation scripts.
- Consider lower-cost hosting options after project completion.
- Consider using a shared App Service Plan across portfolio apps.
- Consider Static Web Apps, Container Apps, Functions, or other hosting models based on future requirements.
- Add cost estimation notes later if needed.
- Add production/staging parameter files if portfolio hosting matures.
