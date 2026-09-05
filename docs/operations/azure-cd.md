# Azure Continuous Deployment (CD)

ContentForge deploys to Azure App Service using GitHub Actions with **OpenID Connect (OIDC) federated credentials** — no long-lived Azure passwords or service principal secrets in the repository.

**Workflow:** [`.github/workflows/deploy.yml`](../../.github/workflows/deploy.yml)  
**Related:** [CI pipeline](./ci.md) · [Azure deployment guide](./azure-deployment.md) · [Azure security](./azure-security.md)

---

## Pipeline overview

```text
resolve-config
      ↓
  validate          ← full CI-equivalent gates (backend, frontend, Bicep, Docker)
      ↓
build-and-publish   ← push immutable images to ACR (:sha + :{env}-latest)
      ↓
    migrate         ← explicit EF Core migrations (Azure SQL)
      ↓
    deploy          ← update App Service containers (optional Bicep)
      ↓
    verify          ← platform health + HTTP probes + image tag confirmation
```

| Stage | Purpose |
|-------|---------|
| **Validate** | Blocks deploy on test, lint, format, Bicep, or Docker build failures |
| **Build and publish** | Produces versioned container images in Azure Container Registry |
| **Migrate** | Applies `ContentForge.Infrastructure.SqlServer` migrations (never on API startup) |
| **Deploy** | Updates running App Service containers; optional infrastructure via Bicep |
| **Verify** | Confirms App Service state, image tags, Web health, and API reachability via Web proxy |

---

## Environments

| GitHub environment | Bicep `environment` | Resource group (default) | Trigger |
|--------------------|---------------------|--------------------------|---------|
| `staging` | `staging` | `rg-contentforge-staging` | Push to `main`, or manual dispatch |
| `production` | `prod` | `rg-contentforge-prod` | Manual dispatch only |

### Production protection (required)

Configure the **`production`** GitHub environment with:

- **Required reviewers** — at least one approver before deploy jobs run
- **Deployment branches** — restrict to `main` (recommended)
- Optional **wait timer** for change windows

The workflow uses `environment: production` on publish, migrate, deploy, and verify jobs so protection rules apply before any Azure changes.

`staging` may run automatically on every merge to `main` (after validation in the same workflow).

---

## Prerequisites (one-time platform setup)

Complete these **before** the first successful workflow run:

1. **Azure Container Registry** (shared across environments or per-env — document your choice)
2. **Resource groups** — created manually or via first Bicep deploy (`rg-contentforge-staging`, `rg-contentforge-prod`)
3. **Bicep parameter files** — use real Azure AD SQL admin values (not Contoso / empty GUID). Template: `prod.example.bicepparam`. Staging/prod must set `acknowledgePublicDataPlaneRisks=true`. CI runs `validate-azure-parameters.sh`.
4. **Initial infrastructure deploy** — run workflow manually with **Deploy infrastructure (Bicep)** enabled, or deploy Bicep locally once
5. **Key Vault secret** — create `jwt-signing-key` after first infrastructure deploy ([azure-deployment.md](./azure-deployment.md#41-jwt-signing-key-key-vault))
6. **SQL grants** — run `infra/azure/scripts/grant-api-sql-access.sql` for the API managed identity (runtime)
7. **Deployment identity SQL access** — add the GitHub Actions app registration (or its group) to the Azure AD SQL admin group, **or** grant `db_ddladmin` to a dedicated migration identity used only by the pipeline
8. **Deploy preflight** (after infra exists) — `infra/azure/scripts/deploy-preflight.sh` with `RESOURCE_GROUP`, `API_APP`, `WEB_APP`
9. **Password reset SMTP** — configure Production App Settings for `PasswordReset__*` (SMTP); local prod Compose uses MailHog

---

## GitHub configuration

### Repository variables

Set under **Settings → Secrets and variables → Actions → Variables**:

| Variable | Description |
|----------|-------------|
| `AZURE_CLIENT_ID` | App registration client ID (federated) |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Target subscription |
| `ACR_NAME` | Azure Container Registry name (not the login server) |
| `AZURE_LOCATION` | Optional; default `westeurope` for new resource groups |
| `AZURE_RESOURCE_GROUP_STAGING` | Optional override; default `rg-contentforge-staging` |
| `AZURE_RESOURCE_GROUP_PRODUCTION` | Optional override; default `rg-contentforge-prod` |

These are identifiers, not credentials. Do **not** store client secrets.

### Federated credential (OIDC)

1. Create an **App registration** (or reuse one) for GitHub Actions deployment.
2. Add **Federated credentials** (issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`).

   Prefer the **repo-id subject format** GitHub Actions currently issues (copy the exact `subject claim` from a failed `azure/login` log if unsure):

   | Purpose | Subject |
   |---------|---------|
   | Staging environment jobs | `repo:<owner>@<ownerId>/<repo>@<repoId>:environment:staging` |
   | Production environment jobs | `repo:<owner>@<ownerId>/<repo>@<repoId>:environment:production` |
   | Optional: pushes to `main` (non-environment jobs) | `repo:<owner>@<ownerId>/<repo>@<repoId>:ref:refs/heads/main` |

   Older subjects without IDs (`repo:<org>/<repo>:environment:staging`) no longer match tokens from this repository and will fail with `AADSTS700213`.

3. Grant Azure RBAC on each resource group (and ACR):

   | Role | Scope | Purpose |
   |------|-------|---------|
   | **Contributor** | `rg-contentforge-{staging\|prod}` | Deploy Bicep, update App Services |
   | **AcrPush** | ACR | Push container images |
   | **User Access Administrator** (or manual AcrPull assignment) | ACR | Grant App Service MIs AcrPull (workflow attempts idempotent assignment) |
   | **SQL Server Contributor** (or custom) | SQL server | Temporary migration firewall rules |

4. Add the deployment identity to the **Azure AD SQL admin group** configured in Bicep parameters so migrations can run with `Authentication=Active Directory Default`.

### What must never be committed

- Service principal client secrets
- JWT signing keys
- SQL passwords
- ACR admin passwords
- Key Vault secret values

Runtime secrets remain in **Key Vault** and App Service Key Vault references (see Bicep).

---

## Running a deployment

### Staging (automatic)

Every push to `main` runs the full deploy pipeline targeting **staging** after validation.

### Staging or production (manual)

1. Open **Actions → Deploy → Run workflow**
2. Select **staging** or **production**
3. Options:
   - **Deploy infrastructure** — run Bicep (first deploy or template changes)
   - **Skip migrations** — container-only rollback or no schema change (use with care)

### First-time production deploy checklist

- [ ] Production GitHub environment protection configured
- [ ] Bicep parameters reviewed for `prod`
- [ ] Run with **Deploy infrastructure** enabled
- [ ] Create Key Vault `jwt-signing-key`
- [ ] Run SQL runtime grant script for API MI
- [ ] Confirm deployment identity can apply migrations
- [ ] Bootstrap first admin user (organizational runbook)
- [ ] Verify Web URL and functional smoke test

---

## Workflow inputs

| Input | Default | Description |
|-------|---------|-------------|
| `environment` | — (required for manual) | `staging` or `production` |
| `deploy_infrastructure` | `false` | Run `az deployment group create` with current image tags |
| `skip_migrations` | `false` | Skip EF migration step |

---

## Verification behavior

The **verify** job checks:

1. App Service **state** = `Running`, **availabilityState** = `Normal`
2. Deployed container image tags match the build (`${{ github.sha }}`)
3. `GET https://{webHost}/health` → 200
4. `GET https://{webHost}/` → HTML response
5. `POST https://{webHost}/api/v1/auth/login` → 401/400/422 (proves API reachable via nginx proxy)

**Note:** Staging and production **block direct public API access** (IP restrictions). Verification intentionally uses the **Web App proxy** (`/api/...`), not the API hostname.

Direct API `/health/ready` checks from GitHub-hosted runners will fail with **403** when restrictions are enabled — this is expected.

---

## Rollback

ContentForge does **not** auto-rollback on verification failure. Operators choose the appropriate layer below.

### Application rollback (fast — preferred)

Redeploy the **previous known-good container image** without changing schema.

**Option A — GitHub Actions**

1. Re-run **Deploy** workflow on the previous commit SHA, **or**
2. Manual dispatch with **Skip migrations** if schema unchanged

**Option B — Azure CLI**

```bash
RG=rg-contentforge-prod
API=app-cf-api-prod
WEB=app-cf-web-prod
ACR=myregistry.azurecr.io
PREVIOUS_SHA=<git-sha-of-last-good-deploy>

az webapp config container set -g $RG -n $API \
  --docker-custom-image-name "$ACR/contentforge-api:$PREVIOUS_SHA"
az webapp config container set -g $RG -n $WEB \
  --docker-custom-image-name "$ACR/contentforge-web:$PREVIOUS_SHA"
az webapp restart -g $RG -n $API
az webapp restart -g $RG -n $WEB
```

Each successful deploy records **previous API/Web image** tags in the workflow job summary for quick reference.

ACR retains images tagged by commit SHA; `{env}-latest` moves forward only on successful publish.

### Database rollback

EF Core **down migrations are not used** in production.

| Scenario | Action |
|----------|--------|
| Bad migration applied | Restore Azure SQL **point-in-time** (PITR) to before migration; see [azure-deployment.md](./azure-deployment.md) |
| Schema hotfix | Forward-fix migration in `ContentForge.Infrastructure.SqlServer`, test in staging, deploy |

**Never** roll back application containers to a build that expects an older schema while the database remains at a newer version.

### Infrastructure rollback

Redeploy Bicep from a previous Git tag:

```bash
git checkout <tag>
az deployment group create \
  --resource-group rg-contentforge-prod \
  --template-file infra/azure/bicep/main.bicep \
  --parameters infra/azure/bicep/parameters/prod.bicepparam \
  --parameters apiContainerImage=... webContainerImage=...
```

Review ARM deployment history in Azure Portal before destructive changes.

### Secrets rollback

Key Vault supports secret **version history**. Restore a previous `jwt-signing-key` version and restart the API App Service.

### Rollback verification

After any rollback:

1. Web `/health` and UI load
2. Admin login succeeds
3. API proxy probe returns expected auth status
4. `/health/ready` via Azure Portal **Diagnose and solve problems** or from an allowed network path
5. Application Insights shows restored request volume without elevated 5xx rates

---

## Migration script

[`infra/azure/scripts/run-azure-sql-migrations.sh`](../../infra/azure/scripts/run-azure-sql-migrations.sh) is invoked by the workflow. See [database-migrations.md](./database-migrations.md) for the full production migration strategy, failure behavior, and rollback limitations.

Summary:

1. Runs `validate-migrations.sh` (safety tests + pending model check)
2. Verifies the database exists (never creates or drops databases)
3. Adds a temporary SQL firewall rule for the runner IP
4. Dry-run lists pending migrations; apply runs `dotnet ef database update`
5. Removes the firewall rule (even on failure)

Local usage (after `az login`):

```bash
./infra/azure/scripts/run-azure-sql-migrations.sh rg-contentforge-staging sql-cf-staging-xxxxx contentforge
```

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---------|--------------|--------|
| `Azure login` fails | Missing/incorrect federated credential | Verify subject claim matches GitHub environment |
| ACR push denied | Missing AcrPush role | Assign on ACR scope |
| Migration auth failure | Deployment identity not SQL admin | Add to SQL admin group |
| Migration network failure | Firewall blocked runner | Script adds temp rule; verify SQL Server Contributor |
| App pull image failure | Missing AcrPull on MI | Workflow assigns; verify manually |
| API 403 from verify | Direct API access restricted | Expected; verify uses Web proxy |
| JWT errors after deploy | Key Vault secret missing | Create `jwt-signing-key`, restart API |
| Container mismatch | Deploy step failed partially, or verify compared against App Service's `DOCKER\|` prefix | Re-run deploy or rollback images; verify step strips `DOCKER\|` |

---

## Related documents

- [azure-deployment.md](./azure-deployment.md) — manual provisioning and post-deploy steps
- [ci.md](./ci.md) — pull request validation (runs in parallel with CD on merge)
- [infra/azure/README.md](../../infra/azure/README.md) — Bicep modules and parameters
- [SPECIFICATIONS.md](../../SPECIFICATIONS.md) §71–79 — CD and deployment requirements
