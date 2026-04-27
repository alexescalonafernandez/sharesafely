# ADR 001: Use ASP.NET Core Razor Pages for the Initial ShareSafely Web Application

## Status
Accepted

## Context
ShareSafely is an Azure-focused portfolio project. The primary learning goals are Azure Blob Storage, SAS links, App Service, Key Vault, Bicep, monitoring, cleanup, and cloud delivery.

The project owner is already a senior software engineer with strong backend and architecture experience. For milestone **B1.0 - Foundation + Local Upload Skeleton**, the project should avoid unnecessary frontend complexity and prioritize backend/cloud delivery concerns.

## Decision
Use **ASP.NET Core Razor Pages** for the initial ShareSafely web application.

Razor Pages provides a straightforward server-rendered approach that is sufficient for an initial file upload interface while keeping implementation complexity low.

## Alternatives considered
- **ASP.NET Core Web API + SPA frontend**: Rejected for the first milestone due to additional frontend architecture, tooling, and integration complexity.
- **ASP.NET Core MVC**: Rejected for now because Razor Pages offers a simpler page-focused model for a small initial app.
- **Blazor**: Rejected for the first milestone to avoid extra UI framework complexity and keep focus on core Azure delivery goals.

## Consequences
- Faster initial delivery.
- Less frontend complexity.
- Easier local skeleton for B1.0.
- Professional and appropriate ASP.NET Core option for a small server-rendered application.
- Decision can be revisited if UI requirements grow significantly.
