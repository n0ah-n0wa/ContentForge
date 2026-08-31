# Docker assets for ContentForge

Production and local development container images.

## Local development (full stack)

From the repository root:

```bash
docker compose up -d --build
```

See the root [README.md](../../README.md) for URLs, default credentials, and reset commands.

Optional overrides: [`.env.development.example`](./.env.development.example)

## Production images

| Image | Dockerfile | Base runtime | Port | User |
|-------|------------|--------------|------|------|
| **contentforge-api** | [`api/Dockerfile`](./api/Dockerfile) | `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` | 8080 | `appuser` (UID 10001) |
| **contentforge-web** | [`web/Dockerfile`](./web/Dockerfile) | `nginxinc/nginx-unprivileged:1.27-alpine` | 8080 | `nginx` (unprivileged) |

### API image

- Multi-stage build: .NET SDK 8.0.424 (pinned via `global.json`) → Alpine ASP.NET runtime
- Deterministic publish: layer-cached restore, Release configuration, `UseAppHost=false`
- **No development secrets** baked in — `appsettings.json` placeholders; production requires environment variables
- Health: Docker `HEALTHCHECK` → `GET /health/live`; readiness via `GET /health/ready` (includes DB + media checks)
- Writable media volume at `/app/App_Data/media` when `Media__Provider=Local`

### Web image

- Multi-stage build: Node 20 Alpine (`npm ci` + lockfile) → nginx unprivileged
- Serves Vue SPA static assets; reverse-proxies `/api/` and `/media-files/` to the API
- `VITE_API_BASE_URL` build arg (default empty = same-origin proxy)
- `API_UPSTREAM` runtime env (default `api:8080`, host:port without scheme) for nginx template substitution
- Health: Docker `HEALTHCHECK` → `GET /health` (nginx-only, independent of API)

## Build locally

From the **repository root** (BuildKit recommended):

```bash
# API
docker build -f infra/docker/api/Dockerfile -t contentforge-api:local .

# Web (same-origin API proxy — typical production layout)
docker build -f infra/docker/web/Dockerfile -t contentforge-web:local .

# Web with explicit API URL (split-host deployment)
docker build -f infra/docker/web/Dockerfile \
  --build-arg VITE_API_BASE_URL=https://api.example.com \
  -t contentforge-web:local .
```

PowerShell:

```powershell
docker build -f infra/docker/api/Dockerfile -t contentforge-api:local .
docker build -f infra/docker/web/Dockerfile -t contentforge-web:local .
```

## Run a production-like stack locally

```bash
cp infra/docker/.env.production.example .env.production
# Edit .env.production — set POSTGRES_PASSWORD and JWT_SIGNING_KEY (min 32 chars)

docker compose -f docker-compose.prod.yml --env-file .env.production build
docker compose -f docker-compose.prod.yml --env-file .env.production up -d postgres

# Apply EF Core migrations (one-time or after schema changes)
docker compose -f docker-compose.prod.yml --env-file .env.production \
  --profile tools run --rm migrate

docker compose -f docker-compose.prod.yml --env-file .env.production up -d
```

Admin UI: `http://localhost:8080` (or `PUBLIC_PORT` from `.env.production`)

## Required environment variables (API)

| Variable | Description |
|----------|-------------|
| `Database__ConnectionString` | PostgreSQL connection string |
| `Jwt__SigningKey` | Symmetric signing key (≥ 32 chars; no dev placeholders in Production) |
| `Jwt__Issuer` | JWT issuer (default: ContentForge) |
| `Jwt__Audience` | JWT audience (default: ContentForge.Admin) |

Optional:

| Variable | Description |
|----------|-------------|
| `Cors__AllowedOrigins__0` | Required when browser calls API directly (not via nginx proxy) |
| `Media__Provider` | `Local` (default in container) or `Azure` |
| `Media__UseManagedIdentity` | `true` for Azure Blob with managed identity |
| `Media__StorageAccountName` / `Media__BlobServiceUri` | Azure storage configuration |
| `ApplicationInsights__ConnectionString` | Enables telemetry in Production |

See [docs/operations/docker.md](../../docs/operations/docker.md) for CI/CD integration and Azure deployment notes.

See [docs/architecture/container-security-review.md](../../docs/architecture/container-security-review.md) for image hardening decisions and verification.

## CI/CD usage

Typical pipeline steps:

1. `docker build` both images with immutable tags (`:${{ github.sha }}`)
2. Run EF migrations using the `migrate` service profile or a dedicated SDK job
3. Push images to registry
4. Deploy API with secrets from vault (Key Vault, GitHub Secrets, etc.)
5. Configure orchestrator HTTP probes on `/health/live` and `/health/ready`

`.dockerignore` excludes tests, docs, dev compose files, and local secrets from build context.

## Development vs production

| Concern | Development (`docker-compose.yml`) | Production images |
|---------|-----------------------------------|-------------------|
| Purpose | Local Postgres + Azurite | Deployable API + SPA |
| Secrets | Fixed dev passwords | Env / secret store only |
| API process | `dotnet run` on host | Published DLL in Alpine |
| Frontend | Vite dev server | nginx static + proxy |
| Swagger | Enabled in Development | Disabled in Production |
