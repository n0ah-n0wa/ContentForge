# ContentForge — Azure Infrastructure (Bicep)

Infrastructure as Code for ContentForge on Azure. This folder defines **development**, **staging**, and **production** environments using [Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview) modules with parameterized configuration, secure defaults, managed identity, and least-privilege RBAC.

**Nothing in this folder deploys automatically.** Templates and documentation only — run provisioning manually when ready.

## Architecture

Each environment deploys into its own resource group:

```text
┌─────────────────────────────────────────────────────────────────┐
│  Resource group: rg-contentforge-{env}                          │
├─────────────────────────────────────────────────────────────────┤
│  Log Analytics ──► Application Insights                         │
│                                                                 │
│  App Service Plan (Linux)                                       │
│    ├── app-cf-api-{env}  (container, system-assigned MI)      │
│    └── app-cf-web-{env}  (container, nginx → API upstream)    │
│                                                                 │
│  Azure SQL Server + database (Azure AD admin, MI auth for API)  │
│  Storage account + private media container (MI blob access)     │
│  Key Vault (RBAC, soft delete, purge protection)                │
└─────────────────────────────────────────────────────────────────┘
```

| Resource | Purpose |
|----------|---------|
| **App Service (API)** | Runs `contentforge-api` container; system-assigned managed identity |
| **App Service (Web)** | Runs `contentforge-web` container; proxies `/api/` to API host |
| **Azure SQL Database** | Production database (`Database:Provider=AzureSQL`) |
| **Blob Storage** | Media binaries (`Media:Provider=Azure`, `UseManagedIdentity=true`) |
| **Application Insights** | Telemetry via `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| **Key Vault** | JWT signing key (`Jwt:SigningKey` Key Vault reference) |
| **Log Analytics** | Centralized logs and App Insights workspace backing |

### Identity and access

| Principal | Permission | Scope |
|-----------|------------|-------|
| API managed identity | **Storage Blob Data Contributor** | Storage account |
| API managed identity | **Key Vault Secrets User** | Key Vault |
| API managed identity | **SQL DB user** (post-deploy) | `contentforge` database |
| Azure AD SQL admin group | **Azure AD administrator** | SQL server |

No SQL passwords or JWT keys are stored in Bicep. Shared key access to storage is disabled.

### Environment defaults

| Setting | dev | staging | prod |
|---------|-----|---------|------|
| App Service Plan | B1 | B2 | P1v3 |
| SQL SKU | Basic (2 GB) | S0 (2 GB) | S1 (32 GB) |
| Storage replication | LRS | LRS | GRS |
| Blob soft delete | No | Yes | Yes |
| Log retention | 30 days | 30 days | 90 days |
| App Insights sampling | 100% | 100% | 50% |
| Key Vault purge protection | No (7-day soft delete) | No | Yes (90-day soft delete) |

Override SKUs via `appServicePlanSku` and `sqlDatabaseSku` parameters in `parameters/*.bicepparam`.

## Prerequisites

1. **Azure subscription** with permissions to create resource groups and deploy resources (`Contributor` on the subscription or target resource groups).

2. **Azure CLI** 2.55+ with Bicep installed:

   ```bash
   az version
   az bicep version
   az bicep install
   ```

3. **Authenticated CLI session**:

   ```bash
   az login
   az account set --subscription "<subscription-id-or-name>"
   ```

4. **Azure AD group** (recommended) for SQL server administration:
   - Create a security group, e.g. `contentforge-sql-admins`
   - Note its **object ID** and use as `azureAdAdminObjectId`
   - Add break-glass operators who may run migrations and grant MI access

5. **Container registry** (Azure Container Registry recommended):
   - Build and push images from `infra/docker/` (see [infra/docker/README.md](../docker/README.md))
   - Grant API/Web App Service **AcrPull** on the registry (post-deploy step below)

6. **Optional — Azure DevOps / GitHub Actions** service principal with deployment rights — see [docs/operations/azure-cd.md](../../docs/operations/azure-cd.md) for OIDC setup and the [Deploy workflow](../../.github/workflows/deploy.yml).

## Repository layout

```text
infra/azure/
├── README.md                 ← this file
├── bicepconfig.json
├── bicep/
│   ├── main.bicep            ← orchestration entry point
│   ├── modules/              ← reusable resource modules
│   └── parameters/
│       ├── dev.bicepparam
│       ├── staging.bicepparam
│       ├── prod.bicepparam
│       └── dev.example.bicepparam
└── scripts/
    ├── grant-api-sql-access.sql
    ├── grant-api-sql-migration.sql
    ├── run-azure-sql-migrations.sh
    ├── validate-migrations.sh
    ├── destructive-migration-allowlist.txt
    └── staging-smoke-test.ps1
```
## Configuration

1. Edit the parameter file for your target environment under `bicep/parameters/`.
2. Replace placeholder values:
   - `azureAdAdminObjectId` — Azure AD group or user object ID
   - `azureAdAdminLogin` — matching UPN or group display name
   - `apiContainerImage` / `webContainerImage` — full image references
   - `containerRegistryUrl` — ACR login server (empty for public Docker Hub)
   - `location` — Azure region (default `westeurope`)
   - `tags` — cost center, owner, etc.

Parameter files contain **no secrets**. Secrets are created in Key Vault after deployment.

## Provisioning steps

> **Do not run these commands until you have reviewed parameters and obtained approval.** This repository intentionally ships templates only.

### 1. Validate templates (safe — no resources created)

```bash
cd infra/azure/bicep

az bicep build --file main.bicep

az deployment group validate \
  --resource-group rg-contentforge-dev \
  --template-file main.bicep \
  --parameters parameters/dev.bicepparam
```

Create the resource group first if it does not exist:

```bash
az group create \
  --name rg-contentforge-dev \
  --location westeurope \
  --tags environment=dev project=contentforge managedBy=bicep
```

Repeat validation for `staging` and `prod` with matching resource group names:
`rg-contentforge-staging`, `rg-contentforge-prod`.

### 2. Deploy infrastructure

```bash
az deployment group create \
  --name contentforge-dev-$(date +%Y%m%d) \
  --resource-group rg-contentforge-dev \
  --template-file main.bicep \
  --parameters parameters/dev.bicepparam
```

Capture deployment outputs:

```bash
az deployment group show \
  --resource-group rg-contentforge-dev \
  --name contentforge-dev-YYYYMMDD \
  --query properties.outputs
```

### 3. Post-deploy configuration (required)

#### a. Create JWT signing key in Key Vault

```bash
KV_NAME="<keyVaultName from outputs>"
JWT_KEY=$(openssl rand -base64 64)

az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name jwt-signing-key \
  --value "$JWT_KEY"
```

The API App Service references `@Microsoft.KeyVault(SecretUri=...)` — restart the API app after the secret exists:

```bash
az webapp restart --resource-group rg-contentforge-dev --name app-cf-api-dev
```

#### b. Grant API managed identity SQL database access

Connect as the Azure AD SQL administrator:

1. **Runtime** (required): run `scripts/grant-api-sql-access.sql` — grants `db_datareader` and `db_datawriter` only.
2. **Migrations** (pipeline): see [docs/operations/database-migrations.md](../../docs/operations/database-migrations.md). Use `scripts/run-azure-sql-migrations.sh` in CI/CD. Prefer a dedicated migration runner identity in production — not the API managed identity.

Replace the placeholder managed identity name (`app-cf-api-dev`) with deployment output `apiAppName`.

```bash
# Example using Azure CLI + AD token (requires sqlcmd or Azure Data Studio)
sqlcmd -S <sqlServerFqdn> -d contentforge -G -i infra/azure/scripts/grant-api-sql-access.sql
```

#### c. Run database migrations

Run EF Core migrations explicitly during deployment (see SPECIFICATIONS.md §78). Example from a migration runner or admin workstation with Azure AD access:

```bash
dotnet ef database update \
  --project src/ContentForge.Infrastructure \
  --startup-project src/ContentForge.Api
```

Set `Database__ConnectionString` locally using Azure AD auth or run from a pipeline identity with DDL rights.

#### d. Container registry pull access

When using ACR, assign **AcrPull** to each App Service managed identity:

```bash
ACR_ID=$(az acr show --name contentforge --query id -o tsv)
API_PRINCIPAL=$(az webapp identity show -g rg-contentforge-dev -n app-cf-api-dev --query principalId -o tsv)

az role assignment create \
  --assignee "$API_PRINCIPAL" \
  --role AcrPull \
  --scope "$ACR_ID"
```

Repeat for the web app if it pulls from the same private registry.

#### e. Web → API proxy (HTTPS)

The web container proxies to the API over **HTTPS** using `API_UPSTREAM_SCHEME`, `API_UPSTREAM_HOST`, and `API_UPSTREAM`. Rebuild and redeploy the web image after changing nginx settings.

#### f. Verify security controls

See [docs/operations/azure-security.md](../../docs/operations/azure-security.md) for the verification checklist (API restriction, audit logs, etc.).

### 4. Smoke test

- API: `https://<apiHostName>/health/ready`
- Web: `https://<webHostName>/`
- Application Insights: confirm requests appear in the portal

## Security notes

See **[docs/operations/azure-security.md](../../docs/operations/azure-security.md)** for the full security model, exposure analysis, and verification checklist.

Key controls in the Bicep templates:

- **API access restriction** (staging/prod): default Deny inbound; allow **AzureCloud** so the Web App can proxy while direct public API access is blocked
- **Storage & Key Vault network ACLs** (staging/prod): default Deny from internet; AzureServices bypass for managed identity and Key Vault references
- **Blob RBAC** scoped to the `media` container only
- **SQL**: Azure AD-only authentication; runtime MI gets reader/writer only (migration DDL via separate script)
- **HTTPS-only** App Service; Web → API over HTTPS; remote debugging disabled
- **Diagnostics & SQL auditing** sent to Log Analytics
- **Backups**: 35-day PITR + long-term retention in production

For network hardening beyond these defaults (private endpoints, VNet integration, WAF), extend the modules incrementally.

## Related documentation

- [docs/operations/azure-deployment.md](../../docs/operations/azure-deployment.md) — full provisioning guide
- [docs/operations/azure-security.md](../../docs/operations/azure-security.md) — security model
- [docs/operations/docker.md](../../docs/operations/docker.md) — building container images
- [docs/operations/azure-storage.md](../../docs/operations/azure-storage.md) — media configuration
- [docs/operations/azure-observability.md](../../docs/operations/azure-observability.md) — Application Insights
- [SPECIFICATIONS.md](../../SPECIFICATIONS.md) §72–79 — deployment requirements

## Phase status

This deliverable corresponds to **IMPLEMENTATION_PLAN.md Phase 16** (Azure infrastructure + CD). Continuous deployment is implemented in [`.github/workflows/deploy.yml`](../../.github/workflows/deploy.yml) (see [docs/operations/azure-cd.md](../../docs/operations/azure-cd.md)). Optional further hardening (private endpoints, Front Door / WAF) is not required for the current Bicep topology.
