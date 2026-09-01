# Continuous Integration (CI)

ContentForge uses GitHub Actions to validate every pull request and every push to `main`. The workflow definition lives at [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml).

## Triggers

| Event | Branches | Behavior |
|-------|----------|----------|
| `pull_request` | All target branches | Full CI pipeline (tests merge commit with base) |
| `push` | `main` | Full CI pipeline |

Concurrent runs for the same PR or branch are cancelled when a newer commit is pushed (`concurrency.cancel-in-progress`).

## Jobs overview

```text
┌─────────────┐  ┌──────────────┐  ┌──────────────────┐
│   Backend   │  │   Frontend   │  │ Infrastructure   │
│  (parallel) │  │  (parallel)  │  │   (parallel)     │
└─────────────┘  └──────────────┘  └──────────────────┘
```

All three jobs run in parallel. The workflow **fails if any job fails**. Steps do not use `continue-on-error` and failures are not suppressed.

---

## Final quality gates

These checks must pass before merge. Any failure blocks the workflow.

### Backend

| Gate | Enforced by | Fails when |
|------|-------------|------------|
| Clean checkout | `actions/checkout@v4` with `clean: true` | N/A (ensures no stale workspace) |
| NuGet restore | `dotnet restore ContentForge.sln` | Missing packages, NU1901–NU1904 audit findings |
| Vulnerable packages | `dotnet list package --vulnerable` + `NuGetAudit` | Known vulnerable dependencies |
| Formatting | `dotnet format --verify-no-changes` (before build) | C# style drift from `.editorconfig` |
| Compile + analyzers | `dotnet build ContentForge.sln -c Release` | Errors, warnings (`TreatWarningsAsErrors`), NetAnalyzers |
| Unit tests | `ContentForge.UnitTests` (166 tests) | Any unit test failure |
| Architecture tests | `ContentForge.ArchitectureTests` (27 tests) | Layer boundary or project reference violations |
| Integration tests | `ContentForge.IntegrationTests` (~244 tests) | API/auth/persistence failures against PostgreSQL 16 |

### Frontend

| Gate | Enforced by | Fails when |
|------|-------------|------------|
| Lockfile install | `npm ci` | Lockfile out of sync with `package.json` |
| npm audit | `npm audit --audit-level=moderate` | Moderate+ advisories |
| Prettier | `npm run format:check` | Formatting drift |
| ESLint | `npm run lint` (`--max-warnings 0`) | Lint errors or warnings |
| TypeScript | `npm run typecheck` | Type errors |
| Vitest | `npm run test` | Unit/component test failures |
| Production bundle | `npm run build:vite` | Vite build failure |

### Infrastructure

| Gate | Enforced by | Fails when |
|------|-------------|------------|
| Bicep compile | `bicep build infra/azure/bicep/main.bicep` (CLI **v0.41.2** from official GitHub release) | Template or analyzer errors |
| Bicep parameters | `bicep build-params infra/azure/bicep/parameters/*.bicepparam` (dev/staging/prod) | Invalid parameter files |
| API Docker build | `infra/docker/api/Dockerfile` | Image build failure |
| Web Docker build | `infra/docker/web/Dockerfile` | Image build failure |
| API container smoke | `GET /health/live` on built image | Container fails to start or respond |
| Web container smoke | `GET /health` on built image | nginx fails to start or respond |

---

## DevOps review notes

### Clean checkout and reproducibility

- Every job uses `actions/checkout@v4` with **`clean: true`** — no leftover files from prior runs on self-hosted reuse (GitHub-hosted runners are ephemeral; this guards against future runner changes).
- **.NET SDK** pinned to `8.0.424` (`global.json`, Docker API image).
- **Node.js** pinned via `frontend/contentforge-web/.node-version` (`20.18.1`).
- **Bicep** pinned to **v0.41.2** (downloaded from [Azure/bicep releases](https://github.com/Azure/bicep/releases) — no third-party setup action). Standalone CLI uses positional file paths (`bicep build <file>`); `az bicep build --file <file>` is Azure CLI syntax only.
- **PostgreSQL** service image pinned to `postgres:16-alpine`.
- Backend builds **`ContentForge.sln`** explicitly so all projects (including `ContentForge.Infrastructure.SqlServer`) compile.

### Dependency caching

| Cache | Scope | Invalidation |
|-------|-------|--------------|
| NuGet | `**/*.csproj`, `Directory.Packages.props`, `global.json` | Dependency or SDK changes |
| npm | `package-lock.json` | Lockfile changes |
| Docker (GHA) | Scoped per image (`contentforge-api`, `contentforge-web`) | Dockerfile/context layer changes |

Caches speed up runs; they do **not** skip validation steps. Docker builds use `pull: true` to refresh base image metadata.

### Secret handling

- The workflow declares **`permissions: contents: read`** only (infrastructure job adds `actions: write` for Docker GHA cache).
- **No repository secrets** are used — intentional for a validation-only pipeline.
- Test database credentials (`contentforge`/`contentforge`) are **non-secret test fixtures** injected via environment variables and service containers; they are not production values and never appear in application source.

### Test isolation

- Integration tests share one xUnit **collection** (`PersistenceTests`) with a **collection fixture** — test classes do not run in parallel against the same database.
- `PostgreSqlPersistenceFixture` uses a **semaphore** around `TRUNCATE ... CASCADE` resets between tests.
- `ASPNETCORE_ENVIRONMENT=Testing` is set at the job level; `ContentForgeWebApplicationFactory` also forces `Testing`.
- `CONTENTFORGE_TEST_DB_CONNECTION` points CI at the GitHub Actions PostgreSQL service on port **5432** (local docker-compose.test uses **5433** intentionally for isolation).
- Hang detection: `--blame-hang-timeout` on all backend test steps.

### Failure behavior

- No `continue-on-error` on quality steps.
- Failed backend test runs upload **TRX artifacts** (`backend-test-results`) for debugging.
- No deployment or image push — failed builds cannot publish broken artifacts to production.

### Artifact handling

| Artifact | When | Retention |
|----------|------|-----------|
| `backend-test-results` (*.trx) | Backend job failure only | GitHub Actions default retention |
| Container images | Built locally on runner | Discarded when job completes (not pushed) |

### Docker builds

- Multi-stage Dockerfiles build from **repository root** context (same as production).
- API image includes `ContentForge.Infrastructure.SqlServer` in the publish output.
- Post-build **smoke tests** verify containers start and respond to health endpoints (liveness only for API — no database required for `/health/live`).

---

## Known gaps (what CI does not catch)

These are documented bypass vectors — broken changes could pass CI if they only affect:

| Gap | Risk | Mitigation outside CI |
|-----|------|------------------------|
| **Azure SQL runtime** | SqlServer migrations compile but are not applied to a real Azure SQL instance | Apply migrations in staging; integration test against Azure SQL in release pipeline |
| **Azurite / Azure Blob** | Blob integration tests skip when Azurite is unavailable | Run Azurite tests locally or in optional nightly job |
| **E2E / browser tests** | No Playwright/Cypress | Manual or future E2E job |
| **API `/health/ready`** | Smoke test uses liveness only | Staging deploy verification |
| **Cross-job Docker compose** | CI does not run full `docker-compose.prod.yml` stack | Staging environment smoke test |
| **Secret / Key Vault references** | No Azure deployment in CI | Deployment checklist in [azure-deployment.md](./azure-deployment.md) |
| **Performance / load** | No load tests | Separate performance review |

---

## Backend job details

**Runner:** `ubuntu-latest`  
**Timeout:** 30 minutes  
**Service:** PostgreSQL 16

Step order is intentional: **format before build** so style failures fail fast without a full compile.

Analyzers run during **Build** via `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, and `EnforceCodeStyleInBuild`.

NuGet audit warnings **NU1901–NU1904** are elevated to errors in `Directory.Build.props`.

## Frontend job details

**Working directory:** `frontend/contentforge-web`

`npm run build:vite` runs after `typecheck` to avoid duplicate `vue-tsc` work while keeping type and bundle validation separate for clearer failure attribution.

## Infrastructure job details

Bicep validation compiles templates locally — **no Azure subscription or deployment**.

Azure SQL migrations are **compiled** in the Backend job; they are **applied** during deployment (see [azure-deployment.md](./azure-deployment.md)).

---

## Running CI locally

**Backend:**

```bash
dotnet restore ContentForge.sln
dotnet list ContentForge.sln package --vulnerable --include-transitive
dotnet format ContentForge.sln --verify-no-changes
dotnet build ContentForge.sln --configuration Release --no-restore
export ASPNETCORE_ENVIRONMENT=Testing
export CONTENTFORGE_TEST_DB_CONNECTION="Host=localhost;Port=5432;Database=contentforge_test;Username=contentforge;Password=contentforge"
dotnet test tests/ContentForge.UnitTests/ContentForge.UnitTests.csproj -c Release --no-build
dotnet test tests/ContentForge.ArchitectureTests/ContentForge.ArchitectureTests.csproj -c Release --no-build
dotnet test tests/ContentForge.IntegrationTests/ContentForge.IntegrationTests.csproj -c Release --no-build
```

**Frontend:**

```bash
cd frontend/contentforge-web
npm ci
npm audit --audit-level=moderate
npm run format:check
npm run lint
npm run typecheck
npm run test
npm run build:vite
```

**Infrastructure:**

```bash
bicep build infra/azure/bicep/main.bicep
bicep build-params infra/azure/bicep/parameters/dev.bicepparam
docker build -f infra/docker/api/Dockerfile -t contentforge-api:local .
docker build -f infra/docker/web/Dockerfile -t contentforge-web:local .
```

---

## Related documents

- [DEVELOPMENT_RULES.md](../DEVELOPMENT_RULES.md) — coding standards and agent rules
- [AGENTS.md](../../AGENTS.md) — quick verification commands
- [azure-deployment.md](./azure-deployment.md) — deployment (not executed in CI)
