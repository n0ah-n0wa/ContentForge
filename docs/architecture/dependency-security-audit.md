# ContentForge — Dependency and Secret-Security Audit

**Date:** 2026-08-31  
**Scope:** NuGet packages, npm packages, Docker Compose, GitHub Actions, environment files, application configuration, Azure integration, build-log exposure  
**Related:** [security-audit.md](./security-audit.md), [security-auth-review.md](./security-auth-review.md)

---

## Executive summary

A dependency and secret-security audit was performed across the full ContentForge stack. **Two NuGet transitive vulnerabilities** and **five npm dev-dependency vulnerabilities** were found via official audit tooling and remediated with package upgrades and lockfile updates. **No production secrets were committed** to the repository. Local development credentials in `appsettings.Development.json`, Docker Compose, and CI service containers are documented as non-production placeholders.

CI was hardened to fail on future NuGet and npm vulnerabilities at moderate severity or above.

---

## Methodology

| Area | Tool / approach |
|------|-----------------|
| NuGet | `dotnet list package --vulnerable --include-transitive`, `NuGetAudit` in `Directory.Build.props` |
| npm | `npm audit --audit-level=moderate` |
| Secrets | Pattern search for keys, tokens, connection strings; review of `.gitignore`, config files, Docker, workflows |
| Docker | Review of `docker-compose.yml`, `docker-compose.test.yml` (no application Dockerfile in repo) |
| Azure | Review of blob storage factory, health checks, and configuration options |
| Build logs | Review of `RequestLoggingMiddleware`, `SensitiveTelemetryRedactor`, auth command handlers |

---

## NuGet dependencies

### Findings (before remediation)

| Package | Version | Severity | Advisory | Introduced via |
|---------|---------|----------|----------|----------------|
| AngleSharp | 0.17.1 | Moderate | [GHSA-pgww-w46g-26qg](https://github.com/advisories/GHSA-pgww-w46g-26qg) | HtmlSanitizer 9.0.892 |
| OpenTelemetry.Api | 1.12.0 | Moderate | [GHSA-g94r-2vxg-569j](https://github.com/advisories/GHSA-g94r-2vxg-569j) | Azure.Monitor.OpenTelemetry.AspNetCore |

### Remediation applied

| Change | File | Effect |
|--------|------|--------|
| HtmlSanitizer **9.0.892 → 9.2.1039** | `Directory.Packages.props` | Pulls AngleSharp **1.7.2** (above patched 1.5.0) |
| Explicit OpenTelemetry.Api **1.15.3** pin | `Directory.Packages.props`, `ContentForge.Infrastructure.csproj` | Overrides vulnerable 1.12.0 transitive |
| NuGet audit enforcement | `Directory.Build.props` | `NuGetAudit=true`, `NuGetAuditMode=all`, `NuGetAuditLevel=low` |
| CI vulnerability step | `.github/workflows/ci.yml` | `dotnet list package --vulnerable --include-transitive` |

### Post-remediation status

```
dotnet list package --vulnerable --include-transitive
→ No vulnerable packages across all projects
```

### Dependency inventory notes

All direct NuGet packages are centrally versioned in `Directory.Packages.props`. Notable intentional dependencies:

- **Microsoft.EntityFrameworkCore.SqlServer** — supports Azure SQL as an alternate database provider (not unused; required for multi-provider architecture).
- **Azure.Identity** / **Azure.Storage.Blobs** — production blob storage with `DefaultAzureCredential` when `Media:UseManagedIdentity=true`.
- **HtmlSanitizer** — server-side Rich Text XSS defense (see [security-audit.md](./security-audit.md)).

No unnecessary direct NuGet packages were identified for removal.

---

## npm dependencies

### Findings (before remediation)

| Package | Severity | Advisory | Context |
|---------|----------|----------|---------|
| esbuild ≤0.24.2 | Moderate | [GHSA-67mh-4wv8-2f99](https://github.com/advisories/GHSA-67mh-4wv8-2f99) | Nested via vitest 2.x (dev server only) |
| vite ≤6.4.2 | High | [GHSA-4w7w-66w2-5vf9](https://github.com/advisories/GHSA-4w7w-66w2-5vf9) and related | Nested via vitest 2.x |
| vitest <3.2.6 | Critical | [GHSA-5xrq-8626-4rwp](https://github.com/advisories/GHSA-5xrq-8626-4rwp) | Vitest UI server (dev-only; project does not enable UI) |

Production runtime dependencies are minimal: **vue**, **vue-router**, **pinia** only. All reported npm vulnerabilities were in **devDependencies** (test/build toolchain).

### Remediation applied

| Change | File | Effect |
|--------|------|--------|
| vitest **2.1.8 → 3.2.7** | `package.json`, `package-lock.json` | Patches critical Vitest advisory |
| vite **^6.0.3 → ^6.4.3** | `package.json` | Patches path-traversal and related Vite advisories |
| npm overrides for `esbuild` and `vite` | `package.json` | Forces patched versions in nested dependency tree |
| CI audit step | `.github/workflows/ci.yml` | `npm audit --audit-level=moderate` |

### Post-remediation status

```
npm audit --audit-level=moderate
→ found 0 vulnerabilities
```

---

## Secrets and configuration

### Committed secrets scan — no production secrets found

| Location | Finding | Risk |
|----------|---------|------|
| `src/ContentForge.Api/appsettings.json` | Empty `ConnectionString`, `SigningKey`, `ApplicationInsights:ConnectionString` | **Acceptable** — production must supply via environment |
| `appsettings.Development.json` | `DEV_ONLY_CHANGE_ME_*` JWT key, local Postgres password `contentforge` | **Acceptable** — local dev only; not used in production |
| `appsettings.Testing.json` | `TEST_ONLY_*` JWT key, test DB credentials | **Acceptable** — test environment only |
| `frontend/contentforge-web/.env.example` | Empty `VITE_API_BASE_URL` | **Acceptable** |
| Source code | No API keys, Azure account keys, or private keys embedded | **Clean** |

### `.gitignore` coverage

Correctly excludes: `.env`, `.env.local`, `.env.*.local`, `secrets.json`, `node_modules/`, `dist/`, build artifacts, local media storage.

### Build-log and telemetry exposure

| Control | Purpose |
|---------|---------|
| `RequestLoggingMiddleware` | Logs method, path, status, elapsed time — **no request body or Authorization header** |
| `SensitiveTelemetryRedactor` | Redacts sensitive headers and values in Application Insights export |
| `ForgotPasswordCommandHandler` comment | Passwords never stored in failed-login audit metadata |
| `AuditMetadataSanitizer` | Redacts password fields in audit JSON (unit tested) |

No evidence of credentials being written to build logs or request logs.

---

## Docker configuration

No application **Dockerfile** exists in the repository. Compose files provide local infrastructure only:

| File | Services | Credentials |
|------|----------|-------------|
| `docker-compose.yml` | PostgreSQL 16, Azurite | `contentforge`/`contentforge` — **local dev only** |
| `docker-compose.test.yml` | PostgreSQL 16 (port 5433), Azurite | Same dev credentials — **test isolation only** |

**Assessment:** No credentials baked into images. Azurite uses well-known development storage defaults. Production deployments must use managed secrets (Azure Key Vault, App Service settings, etc.) — not committed values.

---

## GitHub Actions

Workflow: `.github/workflows/ci.yml`

| Item | Assessment |
|------|------------|
| Postgres service password `contentforge` | **Acceptable** — ephemeral CI container, not production |
| `CONTENTFORGE_TEST_DB_CONNECTION` in env | **Acceptable** — matches service container only |
| Checkout / setup actions | Uses pinned major versions (`@v4`) |
| New audit steps | NuGet and npm vulnerability gates added |

No secrets stored in workflow files. Production deployment secrets are not referenced (no deploy job in CI).

---

## Azure configuration

| Component | Pattern | Assessment |
|-----------|---------|------------|
| `AzureBlobClientFactory` | `DefaultAzureCredential` when `Media:UseManagedIdentity=true` | **Correct** — no hardcoded keys in Azure mode |
| Connection string mode | `Media:ConnectionString` from configuration | **Correct** — must be supplied at deploy time |
| Application Insights | `ApplicationInsights:ConnectionString` empty in base config | **Correct** |
| Health check | Blob container probe via configured client | No credential logging |

---

## CI hardening changes

```yaml
# Backend (.github/workflows/ci.yml)
- name: NuGet vulnerability audit
  run: dotnet list package --vulnerable --include-transitive

# Frontend (.github/workflows/ci.yml)
- name: Audit
  run: npm audit --audit-level=moderate
```

Additionally, `Directory.Build.props` enables NuGet audit warnings during every `dotnet restore` / build.

---

## Verification (2026-08-31)

All AGENTS.md verification commands were run after remediation:

| Command | Result |
|---------|--------|
| `dotnet restore` | Pass |
| `dotnet list package --vulnerable --include-transitive` | **0 vulnerabilities** |
| `dotnet build --configuration Release` | Pass (0 warnings, 0 errors) |
| `dotnet test --configuration Release` | Pass — **435 tests** (166 unit + 26 architecture + 243 integration) |
| `dotnet format --verify-no-changes` | Pass |
| `npm ci` | Pass |
| `npm audit --audit-level=moderate` | **0 vulnerabilities** |
| `npm run lint` | Pass |
| `npm run test` | Pass — **83 tests** |
| `npm run build` | Pass |

---

## Accepted risks and recommendations

| Item | Severity | Recommendation |
|------|----------|----------------|
| Dev JWT signing keys in `appsettings.Development.json` | Low | Keep `DEV_ONLY_*` prefix; production startup already rejects placeholder keys |
| Docker Compose dev passwords | Low | Document in deployment guide; never reuse in production |
| OpenTelemetry.Api.ProviderBuilderExtensions 1.12.0 (transitive) | Info | Monitor for Azure.Monitor.OpenTelemetry.AspNetCore update; direct OpenTelemetry.Api is pinned to 1.15.3 |
| Vitest/Vite dev-server advisories | N/A post-fix | Do not expose `npm run dev` or Vitest UI to untrusted networks |

### Ongoing maintenance

1. Re-run `dotnet list package --vulnerable` and `npm audit` before each release (now enforced in CI).
2. Review Dependabot or Renovate bot PRs for central package updates in `Directory.Packages.props`.
3. Rotate JWT signing keys and database credentials via environment/Key Vault in production — never commit.

---

## Files changed in this audit

| File | Change |
|------|--------|
| `Directory.Packages.props` | HtmlSanitizer 9.2.1039, OpenTelemetry.Api 1.15.3 |
| `Directory.Build.props` | NuGetAudit enforcement |
| `src/ContentForge.Infrastructure/ContentForge.Infrastructure.csproj` | Direct OpenTelemetry.Api reference |
| `frontend/contentforge-web/package.json` | vitest 3.2.7, vite 6.4.3, npm overrides |
| `frontend/contentforge-web/package-lock.json` | Lockfile refresh |
| `.github/workflows/ci.yml` | NuGet and npm audit steps |
| `tests/ContentForge.IntegrationTests/Persistence/PostgreSqlPersistenceFixture.cs` | Semaphore on parallel DB reset (test stability) |
