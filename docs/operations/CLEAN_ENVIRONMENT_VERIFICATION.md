# Clean-environment verification report

**Date:** 2026-09-05 (re-verified)  
**Commit verified:** `910eae3`  
**Method:** Fresh `git clone` via `file://` into an isolated directory; fully isolated package state (`NUGET_PACKAGES`, `npm ci --cache <fresh>`, `PLAYWRIGHT_BROWSERS_PATH` all pointed at empty directories); Docker images built with `--no-cache --pull`; all Compose stacks run under a distinct project name so no pre-existing volumes are reused.

## Host baseline

| Tool | Version used |
|------|----------------|
| OS | Windows 11 Pro (26200), `LongPathsEnabled=0` |
| .NET SDK | 8.0.424 (`global.json`) |
| Node.js | v24.20.0 (project pins 20.19.0 via `.node-version`; engines allow ≥20) |
| npm | 11.19.0 |
| Docker / Compose | 26.1.4 / v2.27.1 |
| Azure CLI | 2.89.1 (`az bicep`) |
| dotnet-ef | 8.0.11 (global tool) |

## Results (final pass at `910eae3` — all PASS)

| # | Check | Result |
|---|--------|--------|
| 1 | Fresh repository checkout (`file://` clone, clean tree) | PASS |
| 2 | `dotnet restore` (empty NuGet cache) + `npm ci` (empty npm cache) | PASS |
| 3 | Docker environment: `compose build --no-cache --pull` + `up -d`, fresh volumes; all services healthy | PASS |
| 4 | DB init: migrations auto-applied on startup (7), seeded admin login 200, nginx `/api` proxy 200, Swagger 200 | PASS |
| 5 | Migrations: `validate-migrations.ps1` — safety/parity tests + `has-pending-model-changes` clean for PostgreSQL **and** Azure SQL | PASS |
| 6 | Backend: `dotnet format --verify-no-changes` + Release build (0 warnings, 0 errors) | PASS |
| 7 | Frontend: `npm audit` (0 vulns) / Prettier / ESLint / vue-tsc / Vite build | PASS |
| 8 | Unit tests: **174/174** | PASS |
| 9 | Integration tests: **247/247** (PostgreSQL 5433 + Azurite 10010) | PASS |
| 10 | Architecture tests: **31/31** | PASS |
| 11 | Frontend unit/component tests: **94/94** (41 files, Vitest) | PASS |
| 12 | Playwright E2E: **16/16** (fresh browser download, isolated E2E stack) | PASS |
| 13 | Docker API + Web image builds (`--no-cache --pull`) | PASS |
| 14 | `az bicep build` + `validate-azure-parameters.ps1` + `docker compose config` for all four compose files | PASS |

No repository defects were found in this pass; no source changes were required.

## Issues found and fixed in the previous pass (`d7d7bc5`)

### 1. Azure Blob upload failed against Azurite (integration)

`LimitedReadStream` rejected synchronous `Read`, but `Azure.Storage.Blobs` `UploadAsync` still uses sync partition reads.  
**Fix:** Buffer through a size-limited copy into `MemoryStream` before `UploadAsync`; keep async-safe `Read` implementation.

### 2. Parallel content updates returned HTTP 500 instead of 409

Race losers hit unique index `IX_ContentVersions_ContentEntryId_VersionNumber` as `DbUpdateException` (23505) rather than `DbUpdateConcurrencyException`.  
**Fix:** Map that specific constraint violation to `ConcurrencyConflictException` in `EfUnitOfWork` (other unique constraints unchanged).

### 3. Windows reproducibility — shell scripts

`validate-migrations.sh` / `validate-azure-parameters.sh` require Bash; Git was installed at `C:\Git` (not Program Files).  
**Fix:** Added `validate-migrations.ps1` and `validate-azure-parameters.ps1`.

### 4. Windows reproducibility — Prettier `format:check`

With `core.autocrlf=true`, a fresh clone checks out CRLF; Prettier default `endOfLine=lf` failed on ~188 files.  
**Fix:** Set Prettier `endOfLine: "auto"`; set `.gitattributes` to `* text=auto eol=lf` for consistent LF working trees where Git honors it.  
**Re-verified at `910eae3`:** a fresh clone on a `core.autocrlf=true` machine passes `format:check` and `dotnet format --verify-no-changes` with no changes.

## Remaining reproducibility notes (not blockers)

1. **Windows MAX_PATH vs. relocated NuGet cache** (new finding, environment-level — no repo change). With `LongPathsEnabled=0`, placing the NuGet package cache (`NUGET_PACKAGES`) under a deep directory breaks the build in a misleading way: `dotnet restore` succeeds (NuGet is long-path aware), but MSBuild assembly resolution then fails with `MSB3106` + `CS0234` ("namespace does not exist") for the packages with the longest paths. The longest package-relative path in this solution is ~155 chars (`microsoft.extensions.diagnostics.healthchecks.entityframeworkcore/8.0.11/lib/net8.0/…EntityFrameworkCore.dll`), so the cache root must stay under roughly 100 characters. The default `%USERPROFILE%\.nuget\packages` is safe; keep custom cache/clone paths short or enable `LongPathsEnabled`.
2. **Node 24 vs `.node-version` 20.19.0** — verification succeeded on Node 24; CI uses 20.19.0. Prefer matching `.node-version` for closest CI parity.
3. **Standalone `bicep` CLI** — not on PATH; `az bicep` works. CI downloads a pinned Bicep binary.
4. **`docker compose -f docker-compose.prod.yml down`** without env file fails interpolation (`POSTGRES_PASSWORD` required) — expected; use `--env-file .env.production`.
5. **Git Bash path** — document or rely on `.ps1` scripts on Windows.

## Commands used (summary)

```bash
git clone <repo> ContentForge-clean-verify        # isolated dir, empty caches
dotnet restore ContentForge.sln                   # NUGET_PACKAGES → fresh dir
cd frontend/contentforge-web && npm ci --cache <fresh> && cd ../..
docker compose -p <fresh-project> build --no-cache --pull
docker compose -p <fresh-project> up -d
docker compose -f docker-compose.test.yml -p <fresh-project>-test up -d
dotnet format ContentForge.sln --verify-no-changes
dotnet build ContentForge.sln --configuration Release
dotnet test tests/ContentForge.UnitTests --configuration Release --no-build
dotnet test tests/ContentForge.ArchitectureTests --configuration Release --no-build
dotnet test tests/ContentForge.IntegrationTests --configuration Release --no-build
# frontend gates per README
docker compose -f docker-compose.e2e.yml -p <fresh-project>-e2e up -d --build
PLAYWRIGHT_BROWSERS_PATH=<fresh> npm run e2e:install && npm run test:e2e
# Windows: powershell -File infra/azure/scripts/validate-*.ps1
az bicep build -f infra/azure/bicep/main.bicep
docker compose -f <each compose file> config --quiet
```
