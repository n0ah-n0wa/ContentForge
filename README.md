# ContentForge

Production-oriented **headless Content Management System** for creating, reviewing, versioning, publishing, and consuming structured content through a versioned REST API.

ContentForge demonstrates production-grade engineering with **.NET 8**, **Vue 3**, **PostgreSQL / Azure SQL**, **Docker**, **GitHub Actions**, and **Microsoft Azure**.

---

## What ContentForge is

| Capability | Description |
|------------|-------------|
| **Headless CMS** | Admin SPA + REST API; consumers use the public JSON API |
| **Structured content** | Dynamic content types and typed fields (text, rich text, media, relations, …) |
| **Lifecycle** | Draft → In review → Published → Unpublished / Archived, with permissions |
| **Versioning** | Immutable version history, compare, and restore |
| **Media library** | Upload, metadata, soft-delete; local disk or Azure Blob / Azurite |
| **AuthZ** | JWT + ASP.NET Identity; roles Administrator, Editor, Author, Viewer with fine-grained permissions |
| **Audit** | Immutable audit log for administrative and content actions |
| **Ops** | Health checks, rate limiting, Application Insights, containerized deploy to Azure |

**Not in scope:** page builders, multi-tenant SaaS, real-time collaboration, marketing automation. See [SPECIFICATIONS.md](./SPECIFICATIONS.md) §3.

---

## Architecture (high level)

```text
┌─────────────────────┐     HTTPS / REST      ┌──────────────────────────┐
│  Vue 3 Admin SPA    │ ───────────────────► │  ContentForge.Api        │
│  (nginx in Docker)  │   /api/v1/…          │  ASP.NET Core + JWT      │
└─────────────────────┘                       └────────────┬─────────────┘
                                                           │
                     ┌─────────────────────────────────────┼─────────────────────┐
                     ▼                                     ▼                     ▼
            ┌────────────────┐                   ┌─────────────────┐    ┌────────────────┐
            │ Application    │                   │ Infrastructure  │    │ Blob / Local   │
            │ + Domain       │◄── ports ─────────│ EF Core, Identity│    │ media storage  │
            └────────────────┘                   │ Search, Jobs    │    └────────────────┘
                                                 └────────┬────────┘
                                                          ▼
                                                 PostgreSQL (local)
                                                 Azure SQL (cloud)
```

Clean Architecture dependency flow: **Api → Application → Domain ← Infrastructure**.  
Details: [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) · ADRs: [docs/decisions/](./docs/decisions/).

---

## Technology stack

| Layer | Technologies |
|-------|----------------|
| Backend | .NET 8, ASP.NET Core, EF Core 8, FluentValidation, ASP.NET Identity, JWT Bearer, Swashbuckle |
| Data | PostgreSQL (dev/test/CI), Azure SQL Database (staging/production) |
| Media | Local filesystem, Azurite, Azure Blob Storage (managed identity in cloud) |
| Frontend | Vue 3, TypeScript, Vite 6, Pinia, Vue Router, Playwright, Vitest |
| Containers | Docker Compose, multi-stage Dockerfiles under `infra/docker/` |
| CI/CD | GitHub Actions (`.github/workflows/ci.yml`, `deploy.yml`) |
| Azure | App Service (Linux containers), ACR, Key Vault, App Insights, Log Analytics, Bicep (`infra/azure/`) |

SDK pin: [`global.json`](./global.json) (`.NET 8`). Frontend requires **Node.js 20+**.

---

## Major features (implemented)

- Content type schema editor (fields, rename/remove with confirmation for destructive changes)
- Content entry editor with validation, optimistic concurrency (`concurrencyToken`)
- Lifecycle transitions and scheduled publish/unpublish (background processor)
- Content versions (list, get, compare, restore)
- Public read API for published content (`/api/v1/public/...`)
- Preview tokens for unpublished content
- Media upload/list/update/delete and `/media-files/...` binary serving
- User administration and role listing
- Dashboard statistics
- Admin search/filter/sort/pagination on content
- Rate limiting on auth, password reset, public API, media upload, preview
- Problem Details–style API errors

---

## Documentation map

| Document | Purpose |
|----------|---------|
| [SPECIFICATIONS.md](./SPECIFICATIONS.md) | Authoritative product & engineering requirements |
| [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) | System architecture (as built) |
| [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md) | Delivery phases (completed) |
| [docs/DEVELOPMENT_RULES.md](./docs/DEVELOPMENT_RULES.md) | Coding and agent rules |
| [docs/api/README.md](./docs/api/README.md) | API overview and examples |
| [docs/decisions/](./docs/decisions/) | Architecture Decision Records |
| [docs/operations/](./docs/operations/) | CI/CD, Docker, Azure, backups, migrations, monitoring |
| [docs/architecture/](./docs/architecture/) | Design reviews and production audits |
| [AGENTS.md](./AGENTS.md) | Guidance for AI coding agents |

For Azure **rollback**, see [docs/operations/azure-cd.md](./docs/operations/azure-cd.md#rollback). For **monitoring**, see [docs/operations/azure-observability.md](./docs/operations/azure-observability.md).

---

## Prerequisites

| Tool | When required |
|------|----------------|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Recommended full-stack local run; required for Compose workflows |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Native backend / tests |
| [Node.js 20+](https://nodejs.org/) | Native frontend / unit tests / Playwright |
| Azure CLI + Bicep | Only for Azure provisioning (`infra/azure/`) |

---

## Local development

### Option A — Docker (recommended)

From the **repository root**:

```bash
docker compose up -d --build
```

| Service | URL / endpoint |
|---------|----------------|
| Admin UI | http://localhost:8080 |
| API Swagger | http://localhost:5080/swagger |
| API health | http://localhost:5080/health/live · `/health/ready` |
| PostgreSQL | `localhost:5432` — user/password/db: `contentforge` |
| Azurite blob | http://localhost:10000 |

**Seeded administrator** (Development, empty database):

| Email | Password |
|-------|----------|
| `admin@contentforge.local` | `AdminPassword123!` |

On first start the API applies **PostgreSQL** EF migrations and seeds the admin user. Data persists in volumes `postgres_data`, `api_media`, `azurite_data`.

```bash
docker compose logs -f api web
docker compose down                 # stop, keep volumes
docker compose down -v              # wipe data
docker compose up -d postgres azurite   # infra only for native dev
```

Optional env file: copy [`infra/docker/.env.development.example`](./infra/docker/.env.development.example) to `.env.development` and run:

```bash
docker compose --env-file .env.development up -d --build
```

More detail: [docs/operations/docker.md](./docs/operations/docker.md) · [infra/docker/README.md](./infra/docker/README.md).

### Option B — Native API + Vite (hot reload)

```bash
docker compose up -d postgres azurite
dotnet run --project src/ContentForge.Api --launch-profile https
```

- HTTPS Swagger: https://localhost:7080/swagger  
- HTTP: http://localhost:5080  

```bash
cd frontend/contentforge-web
npm ci
npm run dev
```

Vite: http://localhost:5173 — proxies `/api` to the API on port 5080 (see `vite.config.ts`). Optional: [`frontend/contentforge-web/.env.example`](./frontend/contentforge-web/.env.example) (`VITE_API_BASE_URL`). For media binaries under `/media-files` during native Vite dev, point `VITE_API_BASE_URL` at the API origin or open the Docker admin UI on port 8080 (nginx proxies both `/api` and `/media-files`).

---

## Docker usage

| Compose file | Purpose | Notable host ports |
|--------------|---------|-------------------|
| `docker-compose.yml` | Full local stack | web `8080`, api `5080`, pg `5432`, Azurite `10000–10002` |
| `docker-compose.prod.yml` | Prod-like local stack (no Azurite; migrate profile) | public web `8080` |
| `docker-compose.test.yml` | Integration-test Postgres + Azurite | pg `5433`, Azurite blob `10010` |
| `docker-compose.e2e.yml` | Isolated E2E stack | web `28080`, api `25080`, pg `25432` |

Images: `infra/docker/api/Dockerfile`, `infra/docker/web/Dockerfile`.

Production-style example (local Compose — still uses PostgreSQL, not Azure SQL):

```bash
cp infra/docker/.env.production.example .env.production
# Required: set non-empty POSTGRES_PASSWORD and JWT_SIGNING_KEY (>= 32 chars;
# JWT must not contain DEV_ONLY, TEST_ONLY, CHANGE_ME, or REPLACE_ME)
docker compose -f docker-compose.prod.yml --env-file .env.production up -d
# Apply schema (API does not migrate on startup in Production):
docker compose -f docker-compose.prod.yml --env-file .env.production run --rm migrate
```

---

## Testing

### Backend

```bash
dotnet restore
dotnet format ContentForge.sln --verify-no-changes
dotnet build ContentForge.sln --configuration Release
dotnet test --configuration Release --no-build
```

Projects: `tests/ContentForge.UnitTests`, `ContentForge.IntegrationTests`, `ContentForge.ArchitectureTests`.  
Integration tests expect PostgreSQL (CI service or `docker-compose.test.yml` on host port **5433**).

### Frontend

```bash
cd frontend/contentforge-web
npm ci
npm run format:check
npm run lint
npm run typecheck
npm run test
npm run build:vite
```

### End-to-end (Playwright)

```bash
# from repository root
docker compose -f docker-compose.e2e.yml up -d --build

cd frontend/contentforge-web
npm run e2e:install
npm run test:e2e
```

Admin UI under E2E: http://localhost:28080 — see [frontend/contentforge-web/e2e/README.md](./frontend/contentforge-web/e2e/README.md).

CI mirrors these gates: [docs/operations/ci.md](./docs/operations/ci.md).

---

## API usage

- **Base path:** `/api/v1`
- **OpenAPI:** Swagger UI in Development/Testing — `/swagger` (JSON: `/swagger/v1/swagger.json`)
- **Binary media:** `/media-files/{storageKey}` (not under `/api/v1`)
- **Health:** `/health/live`, `/health/ready`

Authenticate with `Authorization: Bearer <accessToken>` after `POST /api/v1/auth/login`.

Examples (login, content, public API): **[docs/api/README.md](./docs/api/README.md)**.

---

## Authentication

| Mechanism | Detail |
|-----------|--------|
| Identity | ASP.NET Core Identity (users, password hashing, reset tokens) |
| Tokens | Short-lived **JWT access** + opaque **refresh** tokens |
| Roles | `Administrator`, `Editor`, `Author`, `Viewer` |
| Authorization | Permission policies (e.g. `content.publish`, `user.read`) — UI and API both check; **API is authoritative** |
| Frontend | Tokens in **sessionStorage**; refresh on 401; logout clears session caches |

Password reset endpoints exist (`forgot-password`, `reset-password`).

| Environment | Delivery |
|-------------|----------|
| Development / Testing | `PasswordReset:DeliveryMode=Logging` (token captured/logged; never use in Production) |
| Production | **`DeliveryMode=Smtp` required** (`Host`, `FromAddress`, `PublicAppBaseUrl`) — API refuses to start otherwise |
| Local prod Compose | MailHog SMTP (`mailhog:1025`, UI on host port `8025`) |

Security model (Azure): [docs/operations/azure-security.md](./docs/operations/azure-security.md).
Auth review: [docs/architecture/security-auth-review.md](./docs/architecture/security-auth-review.md).

---

## Azure deployment

Infrastructure is defined in **Bicep** under [`infra/azure/`](./infra/azure/README.md): App Services (API + Web containers), Azure SQL, Blob Storage, Key Vault, Application Insights, Log Analytics.

Continuous deployment: [`.github/workflows/deploy.yml`](./.github/workflows/deploy.yml)

```text
validate → build/push ACR → migrate Azure SQL → deploy App Services → verify health
```

| Guide | Topic |
|-------|--------|
| [docs/operations/azure-deployment.md](./docs/operations/azure-deployment.md) | Deploy steps |
| [docs/operations/azure-cd.md](./docs/operations/azure-cd.md) | GitHub OIDC CD + rollback |
| [docs/operations/azure-infrastructure.md](./docs/operations/azure-infrastructure.md) | Resource layout |
| [docs/operations/database-migrations.md](./docs/operations/database-migrations.md) | EF migrations (no startup migrate in cloud) |
| [docs/operations/azure-observability.md](./docs/operations/azure-observability.md) | Application Insights |
| [docs/operations/backups-and-recovery.md](./docs/operations/backups-and-recovery.md) | Backups & restore |

Staging deploys on push to `main` (after validation). Production is **manual** `workflow_dispatch` with environment protection.

---

## Environment configuration

### API (`appsettings*.json` + env vars)

Important sections (names only):

| Section | Purpose |
|---------|---------|
| `Database:Provider` / `Database:ConnectionString` | `PostgreSQL` or `AzureSQL` |
| `Jwt:*` | Issuer, audience, signing key, lifetimes |
| `Cors:AllowedOrigins` | Browser origins |
| `Media:*` | `Local` or `Azure`; connection / MI settings |
| `ApplicationInsights:*` | Telemetry |
| `RateLimiting:*` | Per-policy limits |
| `ContentPreview:*` | Preview token lifetime |
| `PublicContentCache:*` | Public API caching |
| `ScheduledPublishing:*` | Background publish/unpublish processor |

Docker Compose maps nested config with `__` (e.g. `Database__ConnectionString`, `Jwt__SigningKey`). Examples: [`infra/docker/.env.development.example`](./infra/docker/.env.development.example), [`.env.production.example`](./infra/docker/.env.production.example).

**Never commit production secrets.** Azure stores the JWT signing key in Key Vault (`jwt-signing-key`).

### Frontend

| Variable | Purpose |
|----------|---------|
| `VITE_API_BASE_URL` | Absolute API origin; leave empty for same-origin / Vite proxy |

---

## Troubleshooting

| Symptom | What to check |
|---------|----------------|
| Compose API unhealthy / DB errors | `docker compose logs api`; wait for Postgres healthy; try `docker compose down -v` if password/volume mismatch after changing `POSTGRES_PASSWORD` |
| Swagger 404 | Swagger is enabled for **Development** and **Testing** only — not Production |
| Frontend cannot call API (native) | API on `5080`; Vite proxy; CORS `AllowedOrigins` if calling API origin directly |
| 401 after idle | Access token expired — client should refresh via `/api/v1/auth/refresh`; re-login if refresh fails |
| 403 / Access denied | Missing permission for role; confirm route `meta.permissions` and API policy |
| 409 Conflict | Stale `concurrencyToken` — reload entry and retry |
| Media 404 | Soft-deleted or wrong storage key; Local vs Azure provider mismatch |
| Integration tests fail locally | Start `docker compose -f docker-compose.test.yml up -d` (Postgres **5433**) |
| E2E timeouts | Ensure `docker-compose.e2e.yml` is up; base URL `http://localhost:28080` |
| Azure migrate fails | Identity needs DDL grants; database must exist (Bicep first); see [database-migrations.md](./docs/operations/database-migrations.md) |
| Azure API 403 from browser | Expected in staging/prod when API public access is restricted — use the **Web** hostname (nginx proxy) |

---

## Repository structure

```text
src/                 .NET backend (Domain, Application, Infrastructure, Infrastructure.SqlServer, Api)
frontend/            Vue 3 admin SPA (contentforge-web)
tests/               Unit, integration, architecture tests
docs/                Architecture, API, ADRs, operations, reviews
infra/docker/        Dockerfiles and env examples
infra/azure/         Bicep modules, parameters, ops scripts
.github/workflows/   ci.yml, deploy.yml
docker-compose*.yml  Local, prod-like, test, and E2E stacks
```

---

## License

Private — portfolio / demonstration project.
