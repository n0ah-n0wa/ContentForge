# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

ContentForge is a headless CMS: .NET 8 REST API + Vue 3 admin SPA, PostgreSQL locally / Azure SQL in the cloud, deployed to Azure via Bicep and GitHub Actions.

Authoritative documents (read before non-trivial changes; do not silently redefine requirements):

- `SPECIFICATIONS.md` — product/engineering requirements (source of truth)
- `docs/DEVELOPMENT_RULES.md` — coding, testing, and agent operating rules
- `docs/ARCHITECTURE.md` — as-built architecture; ADRs in `docs/decisions/`
- `AGENTS.md` — condensed agent rules and CI-matching verification commands

## Commands

### Backend (.NET 8, from repo root)

```bash
dotnet restore
dotnet format ContentForge.sln --verify-no-changes   # CI runs format before build
dotnet build ContentForge.sln --configuration Release
dotnet test --configuration Release --no-build        # requires the Release build above
```

Run a single test / project (xUnit):

```bash
dotnet test tests/ContentForge.UnitTests --configuration Release --no-build
dotnet test --configuration Release --no-build --filter "FullyQualifiedName~PublishContent_WhenDraftAndValid"
```

Integration tests need PostgreSQL + Azurite on host port 5433/10010:

```bash
docker compose -f docker-compose.test.yml up -d
```

Connection override: env var `CONTENTFORGE_TEST_DB_CONNECTION` (defaults to `localhost:5433`, db/user/password `contentforge_test`/`contentforge`/`contentforge`).

### Frontend (`frontend/contentforge-web`, Node 20+)

```bash
npm ci
npm run format:check     # prettier
npm run lint             # eslint, --max-warnings 0
npm run typecheck        # vue-tsc
npm run test             # vitest run
npm run build:vite
```

Single unit test: `npx vitest run tests/unit/someFile.spec.ts` (or `npm run test:watch`).

E2E (Playwright, requires the isolated E2E stack — admin UI at `http://localhost:28080`):

```bash
docker compose -f docker-compose.e2e.yml up -d --build   # from repo root
npm run e2e:install && npm run test:e2e                  # from frontend/contentforge-web
```

### Local development

```bash
docker compose up -d --build              # full stack: web :8080, api :5080, pg :5432, Azurite :10000
docker compose up -d postgres azurite     # infra only, then:
dotnet run --project src/ContentForge.Api --launch-profile https
npm run dev                               # Vite :5173, proxies /api to :5080
```

Seeded dev admin: `admin@contentforge.local` / `AdminPassword123!`.

### Migrations & infra validation

Schema changes require migrations in **both** provider projects, generated from the same model state:

```bash
dotnet ef migrations add <Name> --project src/ContentForge.Infrastructure --startup-project src/ContentForge.Api            # PostgreSQL
dotnet ef migrations add <Name> --project src/ContentForge.Infrastructure.SqlServer --startup-project src/ContentForge.Api # Azure SQL
```

CI fails on pending model changes without a committed migration. Validate locally (from repo root):

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File infra/azure/scripts/validate-migrations.ps1        # .sh variants exist for bash
powershell -NoProfile -ExecutionPolicy Bypass -File infra/azure/scripts/validate-azure-parameters.ps1
```

When touching Bicep/Dockerfiles: `bicep build infra/azure/bicep/main.bicep` and `docker build -f infra/docker/api/Dockerfile .` / `web/Dockerfile`.

## Architecture

Clean Architecture with enforced dependency direction (violations fail `ContentForge.ArchitectureTests`):

```
Api → Application → Domain ← Infrastructure
                          ← Infrastructure.SqlServer
```

- **Domain** (`src/ContentForge.Domain`): entities, value objects, permission catalog (`Permissions`, `DefaultRoleDefinitions`), lifecycle/schema rules, typed exceptions (`DomainException`, `NotFoundException`, `ConflictException`, `ForbiddenException`). No ASP.NET/EF/Azure types.
- **Application**: command/query handlers, DTOs, FluentValidation, ports (`IFileStorage`, `IContentSearchService`, `IAuditService`, `ICurrentUserService`), rich-text sanitization. Depends on abstractions only, never Infrastructure concretes.
- **Infrastructure**: EF Core `AppDbContext` + **PostgreSQL migrations**, ASP.NET Identity + JWT, local/Azure blob storage, EF-based search, scheduled-publishing background service, Development-only DB initializer (migrate + seed).
- **Infrastructure.SqlServer**: **Azure SQL migrations only** — same model, second provider. This dual-migration setup is why every schema change touches two projects.
- **Api**: composition root — thin controllers under `/api/v1/*`, global exception middleware mapping typed exceptions to RFC 7807 Problem Details (422 validation, 409 concurrency, 429 rate limit), rate-limiter policies, Swagger (Development/Testing only), health checks.

Key data-model decisions (see `docs/decisions/`):

- Content entries store draft fields as JSON (`DraftDataJson`) validated in Domain/Application against the relational content-type schema; publishing copies the draft into an immutable `PublishedSnapshotJson` that the public API (`/api/v1/public`) reads exclusively.
- Versions are immutable snapshots in `ContentVersions`; optimistic concurrency via `concurrencyToken` → HTTP 409.
- Media metadata is relational; binaries go through `IFileStorage` (`Media:Provider` = `Local` or `Azure`/Azurite), served at `/media-files/{storageKey}` (outside `/api/v1`).
- Migrations run at API startup in Development only; Staging/Production apply them via an explicit pipeline step (never `Database.Migrate()` in prod).

### Frontend (`frontend/contentforge-web/src`)

- HTTP **only** through `src/api/` modules (`client.ts` — native fetch, not Axios; centralized auth headers + refresh-on-401).
- Pinia is deliberately limited to `authStore`, `notificationStore`, `uiStore`; domain state lives in views/composables.
- Route guards enforce auth + `meta.permissions`; permission-based UI hiding is UX only — the API is authoritative for validation and authorization.
- Strict TypeScript; `<script setup lang="ts">` Composition API; no `any` without an inline justification comment.

## Conventions and constraints

- `TreatWarningsAsErrors` is on solution-wide with .NET analyzers and NuGet audit (`Directory.Build.props`); package versions are centralized in `Directory.Packages.props`. Do not suppress warnings or disable analyzers to pass CI.
- Behavior changes require tests (unit + integration + authorization coverage — see DEVELOPMENT_RULES §4). Test naming: `MethodName_StateUnderTest_ExpectedBehavior`.
- Commits: `type(scope): description` (`feat`, `fix`, `test`, `refactor`, `docs`, `chore`, `ci`).
- Sorting/filtering endpoints are whitelist-only; unknown filters → 400. Pagination envelope: `items`/`page`/`pageSize`/`totalItems`/`totalPages`.
- Never log tokens, passwords, connection strings; never expose stack traces or SQL in Production responses.
- Implement incrementally, only what the task requires; report conflicts with SPECIFICATIONS.md instead of silently resolving them.
