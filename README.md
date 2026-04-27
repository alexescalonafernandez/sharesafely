# ShareSafely

## 1. Project overview
ShareSafely is a small web application for secure file sharing, designed with Azure as the target platform. The long-term goal is to support secure uploads, cloud storage, time-limited sharing links, and production-ready deployment/operations on Azure.

## 2. Current milestone: B1.0
**B1.0 - Foundation + Local Upload Skeleton**

This milestone establishes the baseline application structure and local upload workflow for early development.

## 3. What is included in B1.0
- .NET 8 (LTS) application baseline.
- ASP.NET Core Razor Pages UI.
- Local development workflow only.
- Basic file upload form.
- Basic upload validation.
- Configurable local storage path.
- Default local storage path: `App_Data/uploads`.
- No database dependency.

## 4. What is intentionally not included yet
- Azure Blob Storage integration.
- SAS (time-limited link generation).
- Azure App Service deployment configuration.
- Azure Key Vault integration.
- Bicep-based infrastructure automation.
- Monitoring/observability setup.
- Automated blob/file cleanup jobs.

## 5. Tech stack
- .NET 8 LTS
- ASP.NET Core Razor Pages
- Local file system storage (for B1.0 only)

## 6. Local development instructions
1. Install the .NET 8 SDK.
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Run the web app:
   ```bash
   dotnet run --project src/ShareSafely.Web
   ```
4. Open the local URL shown in the console and test file upload.

## 7. Roadmap / next milestones
- **B1.x**: Harden upload flow and validation, improve UX, and prepare integration boundaries.
- **B2.0**: Introduce Azure Blob Storage for persisted file storage.
- **B3.0**: Add SAS-based time-limited share links.
- **B4.0**: Add Azure App Service deployment baseline.
- **B5.0**: Move secrets/configuration into Azure Key Vault.
- **B6.0**: Automate infrastructure with Bicep.
- **B7.0**: Add monitoring and background cleanup for expired/unused files.
