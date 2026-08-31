# Azure Security Model

Security architecture, controls, and residual risks for ContentForge Azure deployments defined in [`infra/azure/`](../../infra/azure/README.md).

## Overview

ContentForge uses **defense in depth** with Azure AD authentication, managed identities, RBAC, network restrictions, and centralized audit logging. Secrets are never stored in Bicep or container images.

```text
Internet
   │
   ▼
┌──────────────────────────────────────────────────────────────┐
│  app-cf-web-{env}  (public)                                  │
│  HTTPS-only · nginx → API over HTTPS (Azure App Service)     │
└────────────────────────────┬─────────────────────────────────┘
                             │ AzureCloud egress
                             ▼
┌──────────────────────────────────────────────────────────────┐
│  app-cf-api-{env}  (restricted in staging/prod)              │
│  Deny public by default · Allow AzureCloud service tag       │
│  System-assigned MI · Key Vault reference for JWT            │
└──────┬─────────────────┬──────────────────────┬──────────────┘
       │ MI + Azure AD   │ MI (container scope) │ MI
       ▼                 ▼                      ▼
  Azure SQL          Blob Storage           Key Vault
  (Azure AD only)    (network Deny)         (network Deny)
```

## Public vs private exposure

| Resource | Internet-facing? | Control |
|----------|------------------|---------|
| **Web App Service** | Yes (by design) | HTTPS-only, nginx security headers, no managed identity |
| **API App Service** | Restricted (staging/prod) | Default **Deny** + **AzureCloud** allow — blocks direct browser/API client access; Web proxy still works |
| **API App Service (dev)** | Open (optional) | `restrictApiPublicAccess=false` or use dev defaults for local testing |
| **Azure SQL** | Public endpoint exists | Azure AD-only auth; `AllowAzureServices` firewall for App Service; no SQL passwords |
| **Blob Storage** | Endpoint exists | Network ACL **Deny** (staging/prod); no public containers; shared keys disabled |
| **Key Vault** | Endpoint exists | Network ACL **Deny** (staging/prod); RBAC; soft delete |
| **Application Insights** | Ingestion public | Query disabled in production (`restrictPublicQuery`) |
| **Log Analytics** | Platform default | Resource-permissions log access enabled |

### Unnecessary public exposure addressed

| Issue | Fix |
|-------|-----|
| API directly reachable from the internet | IP restrictions: default **Deny**, allow **AzureCloud** (staging/prod) |
| Storage/Key Vault open to the internet | Network ACL `defaultAction: Deny`, `bypass: AzureServices` |
| Blob RBAC scoped to entire storage account | RBAC scoped to **`media` container** only |
| API runtime granted `db_ddladmin` | Split scripts: runtime (`db_datareader`/`db_datawriter`) vs migration (`db_ddladmin`) |
| Web → API over HTTP to port 443 | nginx uses `API_UPSTREAM_SCHEME=https` with TLS SNI |
| Deployment outputs leak App Insights connection string | Marked `@secure()` in Bicep outputs |
| Kudu/SCM open on production | SCM default **Deny** + AzureCloud allow (prod) |
| Remote debugging enabled | Disabled on all App Service apps |

### Residual exposure (documented, not yet eliminated)

| Exposure | Why it remains | Mitigation |
|----------|----------------|------------|
| SQL public endpoint + `AllowAzureServices` | App Service MI connectivity without VNet/private endpoint | Azure AD-only authentication; no SQL logins; audit logging |
| AzureCloud service tag on API | Web App outbound IPs are not static on Basic tier | Narrow to Front Door ID header or private endpoints in hardened topology |
| Web App public | Users must reach the admin UI | HTTPS-only; consider WAF (Front Door) in production |
| App Insights ingestion public | App Service must send telemetry | RBAC on query side; sampling in prod |

Follow-on hardening: **private endpoints**, **VNet integration**, and **Azure Front Door + WAF** (see [infra/azure/README.md](../../infra/azure/README.md)).

## Identity and access (managed identities)

### API App Service (system-assigned)

| Target | Role / permission | Scope |
|--------|-------------------|-------|
| Blob Storage | Storage Blob Data Contributor | `media` container only |
| Key Vault | Key Vault Secrets User | Vault (secret: `jwt-signing-key`) |
| Azure SQL | Database user (post-deploy) | `contentforge` database |

The **Web App** has **no** managed identity — it serves static assets and proxies API traffic only.

### Human / pipeline identities

| Identity | Purpose |
|----------|---------|
| Azure AD SQL admin group | Break-glass, migrations, grant MI access |
| Deployment pipeline SP | Infrastructure deploy, optional migration runner |
| Operators | Key Vault secret creation (JWT key), not stored in git |

Post-deploy SQL scripts:

- **Runtime:** [`infra/azure/scripts/grant-api-sql-access.sql`](../../infra/azure/scripts/grant-api-sql-access.sql)
- **Migrations:** [`infra/azure/scripts/grant-api-sql-migration.sql`](../../infra/azure/scripts/grant-api-sql-migration.sql)

Prefer a **dedicated migration runner identity** in production instead of granting `db_ddladmin` to the API managed identity permanently.

## Secrets

| Secret | Storage | Access |
|--------|---------|--------|
| JWT signing key | Key Vault `jwt-signing-key` | API via Key Vault reference (`@Microsoft.KeyVault(SecretUri=...)`) |
| SQL credentials | None | Managed identity + Azure AD |
| Storage account keys | Disabled | `allowSharedKeyAccess: false` |
| App Insights connection string | App Service app setting | Injected at deploy; output marked secure |

**Rules:**

- No secrets in Bicep parameter files, git, or Docker images
- Key Vault: RBAC, soft delete; purge protection enabled outside dev
- Generate JWT key with cryptographically secure random material (see infra/azure README)

## HTTPS and transport security

| Layer | Setting |
|-------|---------|
| App Service (client-facing) | `httpsOnly: true`, TLS 1.2 minimum (site + SCM) |
| Web → API (internal) | HTTPS via `API_UPSTREAM_SCHEME=https`, `proxy_ssl_server_name on` |
| SQL | `Encrypt=True`, TLS 1.2 minimum, `TrustServerCertificate=False` |
| Storage | HTTPS-only, TLS 1.2 minimum |
| FTPS | Disabled |

## Database access

- **Authentication:** Azure AD only (`azureADOnlyAuthentication: true`) — SQL password auth disabled
- **Application connection:** `Authentication=Active Directory Managed Identity`
- **Runtime permissions:** `db_datareader`, `db_datawriter` only
- **Migration permissions:** `db_ddladmin` via separate script/pipeline identity
- **Firewall:** `AllowAzureServices` required until private endpoint topology is adopted
- **Admin access:** Optional per-IP rules in dev only (`allowedAdminIpAddresses`); empty in prod/staging parameter files

## Storage access

- **Public blob access:** Disabled at account and container level
- **Shared key access:** Disabled — only RBAC via managed identity
- **Network:** Default deny from internet (staging/prod); AzureServices bypass for App Service
- **Encryption:** Microsoft-managed keys; infrastructure encryption (double encryption) in production
- **Recovery:** Soft delete + versioning (staging/prod); GRS replication in production

## Access policies and RBAC

| Service | Authorization model |
|---------|---------------------|
| Key Vault | RBAC (`enableRbacAuthorization: true`) — no access policies |
| Storage | Azure RBAC at container scope |
| SQL | Azure AD + database roles |
| Log Analytics / App Insights | Azure RBAC on workspace/component |

No broad roles (e.g. Contributor, Owner) are assigned to application identities.

## Diagnostic logging and auditing

All logs flow to the environment Log Analytics workspace (`log-cf-{env}`):

| Resource | Logs / metrics |
|----------|----------------|
| App Service (API + Web) | All log groups, all metrics |
| Azure SQL | Server auditing + DB diagnostics (security audit, errors, query store) |
| Storage account | Read, write, delete, transaction metrics |
| Key Vault | AuditEvent |

Application Insights is workspace-backed. Production disables public query access on the component.

## Backup configuration

| Component | Configuration |
|-----------|---------------|
| Azure SQL (short-term) | PITR: 7 days (dev/staging), **35 days** (prod) |
| Azure SQL (long-term) | Weekly/monthly/yearly retention (**prod only**) |
| Azure SQL backup storage | Geo-redundant backups in prod |
| Blob storage | Soft delete (3–7 days); versioning when blob protection enabled |
| Key Vault | Soft delete (7 days dev, 90 days prod); purge protection outside dev |

Document and test restore procedures before production go-live (SPECIFICATIONS.md §79).

## Environment matrix

| Control | dev | staging | prod |
|---------|-----|---------|------|
| API public access restriction | Off | On | On |
| Storage/Key Vault network Deny | Off | On | On |
| Key Vault purge protection | Off | On | On |
| SQL LTR backups | Off | Off | On |
| SQL PITR retention | 7 days | 7 days | 35 days |
| Storage infrastructure encryption | Off | Off | On |
| App Insights public query | On | On | Off |
| SCM restricted | Off | Off | On |

Override via parameters: `restrictApiPublicAccess`, SKU overrides, `allowedAdminIpAddresses`.

## Verification checklist

After deployment, confirm:

- [ ] Direct navigation to `https://<apiHostName>/` returns **403** (staging/prod)
- [ ] Web UI loads and API calls succeed via `/api/`
- [ ] API health: `https://<apiHostName>/health/ready` from Azure Cloud Shell succeeds
- [ ] JWT secret exists in Key Vault; API starts without configuration errors
- [ ] Storage public access tests fail from non-Azure networks
- [ ] SQL connection with SQL auth (username/password) fails
- [ ] Log Analytics receives App Service, SQL audit, and Key Vault audit events
- [ ] Deployment outputs do not expose secrets in plain text (secure outputs)

## Related documentation

- [Azure infrastructure provisioning](../../infra/azure/README.md)
- [Azure infrastructure index](./azure-infrastructure.md)
- [Azure Blob Storage (media)](./azure-storage.md)
- [Azure Observability](./azure-observability.md)
- [Container security review](../architecture/container-security-review.md)
