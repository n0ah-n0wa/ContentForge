# Clean-environment verification report

**Date:** 2026-09-05  
**Commit verified (after fixes):** see repository `main` after this report’s companion commit  
**Method:** Fresh `git clone` into `ContentForge-clean-verify` (no prior `bin/`, `obj/`, or `node_modules`), then README/CI commands.

## Host baseline

| Tool | Version used |
|------|----------------|
| OS | Windows 10 (26200) |
| .NET SDK | 8.0.424 (`global.json`) |
| Node.js | v24.20.0 (project pins 20.19.0 via `.node-version`; engines allow ≥20) |
| npm | 11.19.0 |
| Docker | 26.1.4 |
| Azure CLI Bicep | via `az bicep` |
| Git Bash | `C:\Git\bin\bash.exe` (non-default install path) |

## Results (final pass)

| # | Check | Result |
|---|--------|--------|
| 1 | Fresh repository checkout | PASS |
| 2 | `dotnet restore` + `npm ci` + `dotnet-ef` | PASS |
| 3 | `docker compose up -d --build` (dev) + health | PASS |
| 4 | DB init / seeded admin login | PASS |
| 5 | Migrations (`validate-migrations.sh` / `.ps1`) | PASS |
| 6 | Backend format + Release build | PASS |
| 7 | Frontend format / lint / typecheck / Vitest / Vite build | PASS |
| 8 | Unit tests (174) | PASS |
| 9 | Integration tests (247) | PASS |
| 10 | Architecture tests (31) | PASS |
| 11 | Frontend unit/component tests | PASS (included in 7) |
| 12 | Playwright E2E (16) | PASS |
| 13 | Docker API + Web image builds | PASS |
| 14 | Azure parameter script + `az bicep build` / `build-params` | PASS |

## Issues found and fixed

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

## Remaining reproducibility notes (not blockers)

1. **Node 24 vs `.node-version` 20.19.0** — verification succeeded on Node 24; CI uses 20.19.0. Prefer matching `.node-version` for closest CI parity.
2. **Standalone `bicep` CLI** — not on PATH; `az bicep` works. CI downloads a pinned Bicep binary.
3. **`docker compose -f docker-compose.prod.yml down`** without env file fails interpolation (`POSTGRES_PASSWORD` required) — expected; use `--env-file .env.production`.
4. **Git Bash path** — document or rely on `.ps1` scripts on Windows.

## Commands used (summary)

```bash
git clone https://github.com/n0ah-n0wa/ContentForge.git
dotnet restore ContentForge.sln
cd frontend/contentforge-web && npm ci && cd ../..
docker compose up -d --build
docker compose -f docker-compose.test.yml up -d
dotnet format ContentForge.sln --verify-no-changes
dotnet build ContentForge.sln --configuration Release
dotnet test tests/ContentForge.UnitTests --configuration Release --no-build
dotnet test tests/ContentForge.ArchitectureTests --configuration Release --no-build
dotnet test tests/ContentForge.IntegrationTests --configuration Release --no-build
# frontend gates per README
docker compose -f docker-compose.e2e.yml up -d --build && npm run test:e2e
docker build -f infra/docker/api/Dockerfile -t contentforge-api:verify .
docker build -f infra/docker/web/Dockerfile -t contentforge-web:verify .
# Windows: powershell -File infra/azure/scripts/validate-*.ps1
# or Git Bash: ./infra/azure/scripts/validate-*.sh
az bicep build -f infra/azure/bicep/main.bicep
```
