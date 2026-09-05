# Docker Deployment

Production container images are defined under [`infra/docker/`](../../infra/docker/README.md).

## Architecture

```text
                    ┌─────────────────────┐
  Browser ─────────►│  contentforge-web   │
  :8080             │  (nginx unprivileged)│
                    │  /        → SPA      │
                    │  /api/*        → proxy    │
                    │  /media-files/* → proxy    │
                    │  /health       → 200 ok   │
                    └──────────┬──────────┘
                               │ API_UPSTREAM (container :8080)
                               ▼
                    ┌─────────────────────┐
                    │  contentforge-api   │
                    │  (ASP.NET Alpine)   │
                    │  listens :8080      │
                    │  /health/live       │
                    │  /health/ready      │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │  PostgreSQL         │
                    │  (Compose / local)  │
                    └─────────────────────┘
```

**Port mapping (local Compose):** host `5080` → API container `8080`; host `8080` → web container `8080`.

When the admin SPA and API share a host (recommended), leave `VITE_API_BASE_URL` empty at build time. nginx proxies `/api/` and `/media-files/` to the API so browsers never need cross-origin requests.

For split-host deployments (CDN for SPA, dedicated API subdomain), build the web image with `--build-arg VITE_API_BASE_URL=https://api.example.com` and configure `Cors__AllowedOrigins__*` on the API.

## Security properties

- **Non-root**: API runs as UID 10001; nginx uses the official unprivileged image
- **No baked-in secrets**: Production rejects placeholder JWT keys; connection strings come from environment
- **Minimal runtime**: SDK and Node are build stages only; final images are Alpine-based runtimes
- **Health isolation**: Web `/health` does not expose backend state; API readiness checks dependencies without leaking credentials (see `HealthCheckResponseWriter`)

## Database migrations

| Environment | Behavior |
|-------------|----------|
| **Development** (`ASPNETCORE_ENVIRONMENT=Development`, including `docker-compose.yml` / e2e API) | `DevelopmentDatabaseInitializer` runs `MigrateAsync` and seeds the admin user when the database is empty |
| **Production** (`docker-compose.prod.yml` API, Azure Staging/Production) | API does **not** migrate on startup — apply migrations explicitly before rolling out a new API version |

Local prod-like migrate (Compose profile `tools`):

```bash
docker compose -f docker-compose.prod.yml --env-file .env.production run --rm migrate
```

Equivalent host command (PostgreSQL migrations project):

```bash
dotnet ef database update \
  --project src/ContentForge.Infrastructure/ContentForge.Infrastructure.csproj \
  --startup-project src/ContentForge.Api/ContentForge.Api.csproj
```

Azure SQL uses `ContentForge.Infrastructure.SqlServer` via `infra/azure/scripts/run-azure-sql-migrations.sh` — see [database-migrations.md](./database-migrations.md).

## Azure deployment notes

As built (Bicep under `infra/azure/`):

- **API & Web**: Linux **App Service** containers (`app-cf-api-{env}`, `app-cf-web-{env}` with nginx proxy) — not Azure Static Web Apps
- **Database**: **Azure SQL Database** (`Database__Provider=AzureSQL`) — do **not** run Compose Postgres in cloud environments
- **Media**: `Media__Provider=Azure`, managed identity / blob settings — see [azure-storage.md](./azure-storage.md)
- **Secrets / telemetry**: JWT via Key Vault reference; Application Insights connection string via App Service settings

Optional hardening **not** provisioned by default: private endpoints, VNet integration, Azure Front Door / WAF.

## Observability

Structured JSON logs go to stdout (container log driver). Request logging excludes bodies and authorization headers. When Application Insights is configured (`APPLICATIONINSIGHTS_CONNECTION_STRING` / `ApplicationInsights__ConnectionString`), telemetry is exported in Staging/Production per [azure-observability.md](./azure-observability.md).

## Related

- [README.md](../../README.md) — local Docker Compose quick start
- [infra/docker/README.md](../../infra/docker/README.md) — build commands and environment reference
- [azure-cd.md](./azure-cd.md) — GitHub Actions deploy
- [dependency-security-audit.md](../architecture/dependency-security-audit.md) — image and secret hygiene review
