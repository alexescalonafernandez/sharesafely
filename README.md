# ShareSafely

## Project overview
ShareSafely is a small web app for secure file sharing, with Azure as the long-term target platform.

## Current milestone: B1.0 (completed)
B1.0 now provides a **working local upload flow** for development and validation.

### Implemented in B1.0
- .NET 8 LTS
- ASP.NET Core Razor Pages
- Basic upload page
- Upload validation
- Configurable upload settings
- Configurable local storage path
- Default local storage path: `App_Data/uploads`
- Local file storage using safe generated file names
- Unit tests for upload validation and local file storage
- Uploaded local files are Git-ignored (`src/ShareSafely.Web/App_Data/uploads/*`, with `.gitkeep` preserved)

### Not implemented yet
- Azure Blob Storage
- SAS time-limited links
- Azure App Service deployment
- Azure Key Vault
- Bicep
- Monitoring
- Blob cleanup

## Local development
1. Install the .NET 8 SDK.
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Run the web app:
   ```bash
   dotnet run --project src/ShareSafely.Web
   ```

## Roadmap (aligned to ShareSafely goals)
- **B2.0:** Add Azure Blob Storage.
- **B3.0:** Add SAS-based time-limited share links.
- **B4.0:** Add Azure App Service deployment baseline.
- **B5.0:** Move secrets/configuration to Azure Key Vault.
- **B6.0:** Automate infrastructure with Bicep.
- **B7.0:** Add monitoring and cleanup for expired/unused blobs.
