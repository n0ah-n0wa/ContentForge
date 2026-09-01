# Azure Deployment Guide

End-to-end guide to provision and configure ContentForge on Azure from scratch. Infrastructure is defined in Bicep under [`infra/azure/`](../../infra/azure/README.md); this document covers provisioning, application configuration, database migrations, and verification.

**Related:** [Azure security model](./azure-security.md) · [Azure infrastructure index](./azure-infrastructure.md) · [Docker images](../../infra/docker/README.md)

---

## Architecture summary

```text
Users ──HTTPS──► app-cf-web-{env} (nginx + Vue SPA)
                      │ HTTPS proxy /api/, /media-files/
                      ▼
                 app-cf-api-{env} (ASP.NET Core API, managed identity)
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
    Azure SQL     Blob Storage   Key Vault
                  (media)        (JWT key)
                      │
                      ▼
              Application Insights ──► Log Analytics
```

| Component | Technology |
|-----------|------------|
| API | `contentforge-api` container on Linux App Service |
| Web | `contentforge-web` container (nginx, same-origin `/api/`) |
| Database | Azure SQL Database, Azure AD auth, API managed identity |
| Media | Azure Blob Storage (`media` container), MI, served via API `/media-files/` |
| Secrets | Key Vault reference for `Jwt:SigningKey` |
| Telemetry | Application Insights (workspace-backed) |

---

## Prerequisites

### Azure

1. Azure subscription with permission to create resource groups and deploy resources.
2. Azure CLI 2.55+ with Bicep:

   ```bash
   az login
   az account set --subscription "<subscription-id>"
   az bicep install
   ```

3. **Azure AD security group** for SQL administration (recommended), e.g. `contentforge-sql-admins`.
4. **Azure Container Registry** (recommended) for private API/web images.

### Local tooling

| Tool | Purpose |
|------|---------|
| .NET SDK 8 | Build API, run EF migrations |
| Docker | Build and push container images |
| Node.js 20 | Build web image (via Docker multi-stage) |
| `openssl` or similar | Generate JWT signing key |

### Repository

```bash
git clone <repository-url>
cd ContentForge
```

---

## Phase 1 — Configure parameters

1. Copy and edit the environment parameter file:

   | Environment | File | Resource group |
   |-------------|------|----------------|
   | Development | `infra/azure/bicep/parameters/dev.bicepparam` | `rg-contentforge-dev` |
   | Staging | `infra/azure/bicep/parameters/staging.bicepparam` | `rg-contentforge-staging` |
   | Production | `infra/azure/bicep/parameters/prod.bicepparam` | `rg-contentforge-prod` |

2. Replace placeholders (no secrets in these files):

   | Parameter | Example |
   |-----------|---------|
   | `azureAdAdminObjectId` | Object ID of SQL admin group |
   | `azureAdAdminLogin` | `contentforge-sql-admins@contoso.com` |
   | `location` | `westeurope` |
   | `apiContainerImage` | `myregistry.azurecr.io/contentforge-api:1.0.0` |
   | `webContainerImage` | `myregistry.azurecr.io/contentforge-web:1.0.0` |
   | `containerRegistryUrl` | `myregistry.azurecr.io` |

3. Review [security defaults](./azure-security.md) (API access restrictions, network ACLs, etc.).

---

## Phase 2 — Build and push container images

From the repository root:

```bash
# API
docker build -f infra/docker/api/Dockerfile -t myregistry.azurecr.io/contentforge-api:1.0.0 .

# Web — empty VITE_API_BASE_URL for same-origin nginx proxy (recommended on Azure)
docker build -f infra/docker/web/Dockerfile \
  --build-arg VITE_API_BASE_URL= \
  -t myregistry.azurecr.io/contentforge-web:1.0.0 .

az acr login --name myregistry
docker push myregistry.azurecr.io/contentforge-api:1.0.0
docker push myregistry.azurecr.io/contentforge-web:1.0.0
```

Update parameter files with the pushed image tags.

See [infra/docker/README.md](../../infra/docker/README.md) for image details.

---

## Phase 3 — Provision Azure infrastructure

### Validate (no resources created)

```bash
cd infra/azure/bicep

az bicep build --file main.bicep

az group create --name rg-contentforge-dev --location westeurope \
  --tags environment=dev project=contentforge managedBy=bicep

az deployment group validate \
  --resource-group rg-contentforge-dev \
  --template-file main.bicep \
  --parameters parameters/dev.bicepparam
```

### Deploy

```bash
az deployment group create \
  --name contentforge-dev-$(date +%Y%m%d) \
  --resource-group rg-contentforge-dev \
  --template-file main.bicep \
  --parameters parameters/dev.bicepparam
```

### Capture outputs

```bash
az deployment group show \
  --resource-group rg-contentforge-dev \
  --name contentforge-dev-YYYYMMDD \
  --query properties.outputs -o yaml
```

Record: `apiAppName`, `webAppName`, `keyVaultName`, `sqlServerFqdn`, `storageAccountName`, `apiManagedIdentityPrincipalId`.

---

## Phase 4 — Post-deploy platform configuration

### 4.1 JWT signing key (Key Vault)

```bash
KV_NAME="<keyVaultName from outputs>"
JWT_KEY=$(openssl rand -base64 64)

az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name jwt-signing-key \
  --value "$JWT_KEY"
```

Restart the API app after the secret exists:

```bash
az webapp restart --resource-group rg-contentforge-dev --name app-cf-api-dev
```

### 4.2 Container registry pull (AcrPull)

```bash
ACR_ID=$(az acr show --name myregistry --query id -o tsv)
API_PRINCIPAL=$(az webapp identity show -g rg-contentforge-dev -n app-cf-api-dev --query principalId -o tsv)
WEB_PRINCIPAL=$(az webapp identity show -g rg-contentforge-dev -n app-cf-web-dev --query principalId -o tsv 2>/dev/null || true)

az role assignment create --assignee "$API_PRINCIPAL" --role AcrPull --scope "$ACR_ID"
# Web app only needs AcrPull when using a private registry:
# az role assignment create --assignee "$WEB_PRINCIPAL" --role AcrPull --scope "$ACR_ID"
```

### 4.3 SQL managed identity — runtime access

Connect as the Azure AD SQL administrator and run [`infra/azure/scripts/grant-api-sql-access.sql`](../../infra/azure/scripts/grant-api-sql-access.sql), replacing `app-cf-api-dev` with output `apiAppName`.

This grants `db_datareader` and `db_datawriter` only (least privilege).

---

## Phase 5 — Database migrations

See **[database-migrations.md](./database-migrations.md)** for the production migration strategy, validation tooling, pipeline behavior, failure handling, and rollback limitations.

Summary:

| Provider | Migrations project | Used by |
|----------|-------------------|---------|
| PostgreSQL | `ContentForge.Infrastructure` | Local dev, Docker Compose, CI |
| Azure SQL | `ContentForge.Infrastructure.SqlServer` | Azure deployments |

Migrations are **never applied automatically** on API startup in Production/Staging (SPEC §78).

### Option A — Pipeline runner (recommended)

Use a deployment identity with DDL rights (separate from the API runtime identity):

1. Run [`grant-api-sql-migration.sql`](../../infra/azure/scripts/grant-api-sql-migration.sql) for a migration service principal, **or** connect as the Azure AD SQL admin.
2. Apply migrations:

```bash
export Database__Provider=AzureSQL
export Database__ConnectionString="Server=tcp:<sqlServerFqdn>,1433;Database=contentforge;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Default;"

dotnet ef database update \
  --project src/ContentForge.Infrastructure.SqlServer \
  --startup-project src/ContentForge.Api \
  --context AppDbContext
```

Use `Active Directory Default` when running from an authenticated operator session or pipeline agent with Azure AD access.

### Option API managed identity (dev only)

For a dev environment you may temporarily grant `db_ddladmin` to the API identity via `grant-api-sql-migration.sql`, run `database update`, then revoke DDL rights.

### Option B — Azure DevOps / GitHub Actions step

Run the same `dotnet ef database update` command in a pipeline job that:

- Authenticates with Azure (`az login` / federated credentials)
- Uses `--project src/ContentForge.Infrastructure.SqlServer`
- Runs **before** or **during** deployment, not on every API restart

---

## Phase 6 — Application configuration reference

Bicep injects these API settings automatically. Override via App Service configuration or Key Vault references — **do not hardcode secrets in source control**.

### Required API settings

| Setting | Azure value | Notes |
|---------|-------------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` or `Staging` | Enables production behaviors |
| `Database__Provider` | `AzureSQL` | |
| `Database__ConnectionString` | MI + Azure AD string | Set by Bicep |
| `Jwt__SigningKey` | Key Vault reference | Post-deploy secret |
| `Jwt__Issuer` | `ContentForge` | |
| `Jwt__Audience` | `ContentForge.Admin` | Must match issued tokens |
| `Media__Provider` | `Azure` | |
| `Media__UseManagedIdentity` | `true` | |
| `Media__StorageAccountName` | From deployment | |
| `Media__ContainerName` | `media` | |
| `Media__PublicBaseUrl` | `/media-files` | Same-origin via web nginx |
| `WEBSITES_PORT` | `8080` | |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | From App Insights | Auto-injected |

### Web app settings

| Setting | Azure value |
|---------|-------------|
| `API_UPSTREAM` | `{apiHostName}:443` |
| `API_UPSTREAM_SCHEME` | `https` |
| `API_UPSTREAM_HOST` | `{apiHostName}` |
| `WEBSITES_PORT` | `8080` |

### Optional settings

| Setting | When to set |
|---------|-------------|
| `Cors__AllowedOrigins__0` | Split-host deployment (CDN UI + API subdomain) — not needed for same-origin nginx proxy |
| `ApplicationInsights__SamplingRatio` | `0.5` in prod (Bicep sets from environment defaults) |
| `Media__PublicBaseUrl` | CDN URL when fronting blobs with Azure CDN |

### Configuration behavior

- **No secrets in appsettings.json** — empty placeholders only; production secrets via Key Vault / App Service.
- **`appsettings.Production.json`** sets safe defaults (`Media:PublicBaseUrl`); provider and connection come from environment.
- **Forwarded headers** — enabled automatically in Staging/Production for App Service reverse proxy (HTTPS, client IP).
- **CORS** — disabled when `Cors:AllowedOrigins` is empty (default for same-origin web + API layout).
- **HTTPS** — App Service `httpsOnly`; API uses forwarded `X-Forwarded-Proto`; web proxies to API over HTTPS.

---

## Phase 7 — Seed initial admin (first deploy)

Production does not auto-seed users. Create the first administrator:

1. Run migrations (Phase 5).
2. Use a one-time script or direct SQL insert via Azure AD admin, **or** temporarily deploy with a controlled seed step in your pipeline.
3. For **dev** environments only, local Docker uses `DevelopmentDatabaseInitializer` — this does **not** run on Azure (`ASPNETCORE_ENVIRONMENT` is `Staging` or `Production`).

Document your organization's admin bootstrap procedure in your runbook.

---

## Phase 8 — Verification

### Health checks

```bash
# API readiness (DB + storage)
curl -s "https://<apiHostName>/health/ready"

# API liveness
curl -s "https://<apiHostName>/health/live"

# Web container
curl -s "https://<webHostName>/health"
```

App Service health check path: `/health/ready` (API), `/` (Web).

### Functional smoke test

1. Open `https://<webHostName>/` — admin UI loads.
2. Sign in with bootstrap admin credentials.
3. Upload a media asset — URL should start with `/media-files/`.
4. Open the media URL in a browser — file downloads without auth (public delivery path).
5. Confirm requests in Application Insights.

### Security checklist

See [azure-security.md](./azure-security.md) — verify API direct access blocked (403) in staging/prod, audit logs flowing to Log Analytics.

---

## Environment matrix

| Aspect | dev | staging | prod |
|--------|-----|---------|------|
| Resource group | `rg-contentforge-dev` | `rg-contentforge-staging` | `rg-contentforge-prod` |
| `ASPNETCORE_ENVIRONMENT` | `Staging` | `Staging` | `Production` |
| API public restriction | Off | On | On |
| SQL PITR | 7 days | 7 days | 35 days |
| SQL LTR | No | No | Yes |
| App Insights sampling | 100% | 100% | 50% |

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---------|--------------|--------|
| API fails startup — JWT | Key Vault secret missing | Create `jwt-signing-key`, restart app |
| API fails startup — database | MI not granted SQL access | Run `grant-api-sql-access.sql` |
| `403` on `/health/ready` storage | Blob RBAC or network ACL | Verify MI has Blob Data Contributor on `media` container |
| Login works locally, fails on Azure | Audience/issuer mismatch | Ensure `Jwt__Audience=ContentForge.Admin` |
| Web UI loads, API calls fail | nginx upstream / API restriction | Check `API_UPSTREAM_*` settings; verify web egress allowed by API IP rules |
| Migrations fail | Wrong project or auth | Use `ContentForge.Infrastructure.SqlServer`; Azure AD auth |
| Media upload OK, file 404 | Storage key / serving | Confirm `/media-files/{key}` endpoint; check blob exists |

---

## CI/CD notes

GitHub Actions provides two workflows:

| Workflow | File | Purpose |
|----------|------|---------|
| **CI** | [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) | Validates pull requests and pushes ([ci.md](./ci.md)) |
| **Deploy** | [`.github/workflows/deploy.yml`](../../.github/workflows/deploy.yml) | Builds, publishes, migrates, deploys, and verifies Azure environments ([azure-cd.md](./azure-cd.md)) |

**CI** validates on pull requests and pushes to `main`:

- **Backend:** restore, build (analyzers), format, unit/architecture/integration tests (PostgreSQL)
- **Frontend:** lint, typecheck, Vitest, production build
- **Infrastructure:** Bicep compile + parameter validation, API and Web Docker builds

**Deploy** runs on push to `main` (staging) or manual dispatch (staging/production). It repeats validation, pushes images to ACR via OIDC, applies Azure SQL migrations explicitly, updates App Service containers, and verifies health. Production requires GitHub environment approval.

Azure SQL migrations live in `ContentForge.Infrastructure.SqlServer` and are **compiled** in CI; they are **applied** during deployment, not on API startup. When schema changes, update **both** migration sets:

```bash
# PostgreSQL (local/CI)
dotnet ef migrations add <Name> --project src/ContentForge.Infrastructure --startup-project src/ContentForge.Api

# Azure SQL (after model change, regenerate or add parallel migration)
dotnet ef migrations add <Name> --project src/ContentForge.Infrastructure.SqlServer --startup-project src/ContentForge.Api
```

---

## Related documents

- [infra/azure/README.md](../../infra/azure/README.md) — Bicep templates and module details
- [azure-security.md](./azure-security.md) — security model and exposure analysis
- [azure-storage.md](./azure-storage.md) — blob media configuration
- [azure-observability.md](./azure-observability.md) — Application Insights
- [docker.md](./docker.md) — local and container workflows
- [SPECIFICATIONS.md](../../SPECIFICATIONS.md) §72–79 — deployment requirements
