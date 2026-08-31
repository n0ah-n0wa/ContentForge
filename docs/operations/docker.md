# Docker Deployment

Production container images are defined under [`infra/docker/`](../../infra/docker/README.md).

## Architecture

```text
                    ┌─────────────────────┐
  Browser ─────────►│  contentforge-web   │
  :8080             │  (nginx unprivileged)│
                    │  /        → SPA      │
                    │  /api/*   → proxy    │
                    │  /health  → 200 ok   │
                    └──────────┬──────────┘
                               │ API_UPSTREAM
                               ▼
                    ┌─────────────────────┐
                    │  contentforge-api   │
                    │  (ASP.NET Alpine)   │
                    │  /health/live       │
                    │  /health/ready      │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │  PostgreSQL         │
                    └─────────────────────┘
```

When the admin SPA and API share a host (recommended), leave `VITE_API_BASE_URL` empty at build time. nginx proxies `/api` to the API service so browsers never need cross-origin requests.

For split-host deployments (CDN for SPA, dedicated API subdomain), build the web image with `--build-arg VITE_API_BASE_URL=https://api.example.com` and configure `Cors__AllowedOrigins__*` on the API.

## Security properties

- **Non-root**: API runs as UID 10001; nginx uses the official unprivileged image
- **No baked-in secrets**: Production rejects placeholder JWT keys; connection strings come from environment
- **Minimal runtime**: SDK and Node are build stages only; final images are Alpine-based runtimes
- **Health isolation**: Web `/health` does not expose backend state; API readiness checks dependencies without leaking credentials (see `HealthCheckResponseWriter`)

## Database migrations

Migrations are **not** run automatically on API startup. Apply them in CI/CD before rolling out a new API version:

```bash
dotnet ef database update \
  --project src/ContentForge.Infrastructure/ContentForge.Infrastructure.csproj \
  --startup-project src/ContentForge.Api/ContentForge.Api.csproj
```

The `docker-compose.prod.yml` `migrate` profile wraps this for local validation.

## Azure deployment notes

- **API**: App Service container or Azure Container Apps — inject `Database__ConnectionString`, `Jwt__SigningKey`, and Application Insights connection string via Key Vault references
- **Media**: Set `Media__Provider=Azure`, `Media__UseManagedIdentity=true`, and `Media__StorageAccountName` (see [azure-storage.md](./azure-storage.md))
- **Web**: App Service static web app, Container Apps, or Azure Front Door origin — build with appropriate `VITE_API_BASE_URL` if API is on a separate host
- **PostgreSQL**: Use Azure Database for PostgreSQL; do not run the compose Postgres service in production

## Observability

Structured JSON logs go to stdout (container log driver). Request logging excludes bodies and authorization headers. When `ApplicationInsights__ConnectionString` is set, telemetry is exported automatically in Production.

## Related

- [README.md](../../README.md) — local Docker Compose quick start
- [infra/docker/README.md](../../infra/docker/README.md) — build commands and environment reference
- [dependency-security-audit.md](../architecture/dependency-security-audit.md) — image and secret hygiene review
