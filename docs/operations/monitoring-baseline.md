# Monitoring Baseline (B7.0)

## 1. Purpose
This document records the **B7.0 monitoring and operational visibility baseline** for ShareSafely.

The goal is to establish practical, repeatable visibility into runtime behavior using Azure-native tooling, with **Application Insights as the current baseline** (not a full production monitoring strategy).

## 2. Current monitoring baseline
At this milestone, monitoring is based on:
- Azure App Service runtime/log stream inspection via Azure CLI.
- Application Insights telemetry collection from the app.
- KQL validation in Application Insights Logs.

This baseline confirms the application can be observed during operation and troubleshooting.

## 3. App Service log inspection
Initial App Service logging state was inspected using Azure CLI.

Observed from `az webapp log show`:
- Application logging (filesystem): Off
- Web server logging: Off
- Detailed error messages: Off
- Failed request tracing: Off

Observed from `az webapp log tail`:
- Successful connection to runtime/container log stream.
- Runtime logs visible in near real time.

App state was also confirmed as **Running**.

## 4. Application Insights setup
Application Insights was created for this environment:
- `appi-sharesafely-dev-we-01`

The Web App is configured with:
- `APPLICATIONINSIGHTS_CONNECTION_STRING`

This enables telemetry emission from the application to Application Insights.

## 5. Explicit SDK instrumentation
ShareSafely uses **explicit SDK instrumentation** in the ASP.NET Core app.

Implemented changes:
- Package reference added: `Microsoft.ApplicationInsights.AspNetCore`
- Service registration in [`Program.cs`](https://github.com/alexescalonafernandez/sharesafely/blob/main/src/ShareSafely.Web/Program.cs):
  - `builder.Services.AddApplicationInsightsTelemetry();`

Important operational note:
- The App Service portal may still display **“Turn on Application Insights”**.
- That message applies to App Service automatic instrumentation.
- ShareSafely uses explicit SDK instrumentation; therefore, **Application Insights Logs (KQL) are the source of truth**.

## 6. Bicep-managed monitoring resources
Monitoring baseline resources are now managed by Bicep (notably [`infra/bicep/modules/monitoring.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/monitoring.bicep) and [`infra/bicep/modules/webapp.bicep`](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/webapp.bicep)):

- `Microsoft.Insights/components` (Application Insights)
- Web App application setting for `APPLICATIONINSIGHTS_CONNECTION_STRING`
- Web App hidden-link tag preserved:
  - `hidden-link: /app-insights-resource-id`

This keeps monitoring configuration aligned with infrastructure-as-code.

## 7. Validation with KQL
Telemetry was validated post-deployment using KQL in Application Insights Logs.

Primary query used:
```kusto
requests
| order by timestamp desc
| take 20
```

Additional useful queries:
```kusto
traces
| order by timestamp desc
| take 20
```

```kusto
exceptions
| order by timestamp desc
| take 20
```

Validation outcome:
- Request telemetry was present.
- Application activity was observable after deployment and functional testing.

## 8. Operational troubleshooting workflow
Recommended lightweight workflow:
1. Confirm app health/state in App Service (Running).
2. Tail runtime logs with `az webapp log tail` for immediate symptoms.
3. Open Application Insights Logs and run recent `requests`, `traces`, and `exceptions` queries.
4. Correlate request timing and failures with recent deployment/activity.
5. Re-test key user flow:
   - App load
   - Upload
   - Blob write/read path
   - SAS link generation

Use KQL results as the primary evidence for telemetry health.

## 9. Known observations
Observed warning in log stream:
- `Failed to determine the https port for redirect.`

This was not treated as an incident for this milestone because App Service enforces HTTPS at the platform level (`httpsOnly: true`).

## 10. What is not implemented yet
The following are **not** part of this baseline:
- Alert rules / action groups.
- Dashboards / workbooks.
- Log Analytics diagnostic settings (unless added in a future milestone).
- Storage diagnostic settings.
- Automated telemetry retention/lifecycle tuning or cleanup policies.

## 11. Next milestone
Recommended next step is to move from visibility baseline to actionable operations, for example:
- Define a small set of alert rules for error rate, failed requests, and availability.
- Add an operations dashboard/workbook for daily triage.
- Add documented incident response runbooks tied to telemetry signals.

These items should be introduced explicitly in a later milestone and tracked as infrastructure and operational code where applicable.
