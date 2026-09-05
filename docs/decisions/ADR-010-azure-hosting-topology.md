# ADR-010: Azure Hosting Topology

**Status:** Accepted  
**Date:** 2026-08-31

## Context

ContentForge needs a cloud layout that is secure by default, container-friendly, and operable with managed identity—without requiring VNet/private endpoints for the initial topology.

## Decision

Per environment resource group (`rg-contentforge-{env}`), deploy with Bicep:

- Linux **App Service** plan hosting two containers: `app-cf-api-{env}` and `app-cf-web-{env}`
- **Web** is public (HTTPS); nginx proxies `/api` and media to the API over HTTPS
- **API** public access restricted in staging/prod (Deny + AzureCloud allow)
- **Azure SQL** with Azure AD-only auth; API connects via managed identity
- **Blob Storage** private media container; MI RBAC; shared keys disabled
- **Key Vault** for JWT signing key (Key Vault reference on App Service)
- **Application Insights** + **Log Analytics** for observability
- Images published to **Azure Container Registry**; deploy workflow updates App Service image tags

## Consequences

- Simple two-app topology suitable for portfolio/production demo scale
- Residual exposures documented (SQL public endpoint + AllowAzureServices, AzureCloud tag) until private endpoints/Front Door are adopted
- Web has no managed identity (static + proxy only)
