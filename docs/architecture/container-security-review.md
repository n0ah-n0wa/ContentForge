# ContentForge — Container Security and Reliability Review

**Date:** 2026-09-01  
**Scope:** Production Dockerfiles, Compose stacks, build context, runtime hardening  
**Related:** [dependency-security-audit.md](./dependency-security-audit.md), [docs/operations/docker.md](../operations/docker.md)

---

## Executive summary

Production container images were reviewed for security and reliability. One **concrete secret-exposure issue** was fixed: Development and Testing `appsettings` files were included in the published API output and therefore in production images. Additional hardening was applied to base-image selection, build context, nginx headers, and production Compose runtime constraints.

Both production images were rebuilt from a clean environment and CI verification passed.

---

## Review findings and decisions

### Root / non-root execution

| Image | User | Decision |
|-------|------|----------|
| **contentforge-api** | `appuser` (UID 10001) | Created in runtime stage; `USER appuser` before `ENTRYPOINT` |
| **contentforge-web** | `nginx` (UID 101) | Official `nginxinc/nginx-unprivileged` image — never runs as root |

**Decision:** Keep explicit non-root users. Do not run entrypoints as root and drop privileges later — start unprivileged.

---

### Base image selection

| Stage | Image | Rationale |
|-------|-------|-----------|
| API build | `mcr.microsoft.com/dotnet/sdk:8.0.424-noble-amd64` | Pinned to `global.json` SDK version |
| API runtime | `mcr.microsoft.com/dotnet/aspnet:8.0-alpine-amd64` | Minimal Microsoft-maintained ASP.NET runtime |
| Web build | `node:20-alpine3.21` | Pinned Alpine variant; build stage only |
| Web runtime | `nginxinc/nginx-unprivileged:1.27-alpine` | Static serving + reverse proxy without root |

**Decision:** Alpine-based runtimes for smaller attack surface. Chiseled .NET images were not adopted because they lack shell/`wget` for Docker `HEALTHCHECK` without extra tooling; orchestrator HTTP probes remain the primary production health mechanism.

**Not chosen:** `node:20-alpine` floating tag — pinned to `20-alpine3.21` for reproducibility.

---

### Image size

| Image | Approximate size (local build) | Size controls |
|-------|-------------------------------|---------------|
| contentforge-api | ~148 MB (was ~178 MB with `icu-data-full`) | Multi-stage build; SDK not in runtime; `icu-libs` only |
| contentforge-web | ~48 MB | Multi-stage; only `/dist` copied to nginx |

**Fix applied:** Removed `icu-data-full` from API runtime — `icu-libs` alone is sufficient for PostgreSQL/globalization and saves tens of MB.

---

### Secrets in images

| Finding | Severity | Fix |
|---------|----------|-----|
| `appsettings.Development.json` and `appsettings.Testing.json` published into API image | **High** | `CopyToPublishDirectory=Never` in `ContentForge.Api.csproj` + explicit `rm` in Dockerfile build stage |
| Dev JWT key (`DEV_ONLY_*`) reachable if Development config shipped | **High** | Resolved by publish exclusion |
| Production secrets in Compose files | None in prod stack | `JWT_SIGNING_KEY` and `POSTGRES_PASSWORD` required via env / secret store |

**Verification:** After fix, `dotnet publish` output contains only `appsettings.json` and `appsettings.Production.json` (no secrets).

---

### Exposed ports

| Stack | Exposed to host | Assessment |
|-------|-----------------|------------|
| **docker-compose.prod.yml** | Web `:8080` only | API uses `expose: 8080` (internal network only) — **correct for production-like layout** |
| **docker-compose.yml** (dev) | Web `:8080`, API `:5080`, Postgres `:5432`, Azurite `:10000–10002` | Acceptable for local development |
| **docker-compose.test.yml** | Postgres `:5433`, Azurite `:10010` | Isolated test project; acceptable |

**Decision:** Production Compose must not publish API or database ports to the host.

---

### Writable directories

| Container | Writable paths | Control |
|-----------|----------------|---------|
| API | `/app/App_Data/media` | Named volume `api_media` |
| API | `/tmp`, `/app/.aspnet` | `tmpfs` when `read_only: true` (prod compose; DataProtection keys) |
| Web | `/tmp`, `/var/cache/nginx`, `/var/run` | `tmpfs` when `read_only: true` (prod compose) |
| Postgres / Azurite | Data directories | Named volumes (dev/test only) |

**Decision:** Production Compose enables `read_only: true` on API and web with explicit `tmpfs` and volume mounts for required writes.

---

### Dependency installation (runtime)

| Image | Packages added | Justification |
|-------|----------------|---------------|
| API | `icu-libs`, `wget` | Globalization; Docker `HEALTHCHECK` only |
| Web | None (base image only) | nginx unprivileged includes required tooling |

**Decision:** Minimize runtime packages. `wget` retained solely for health checks; prefer Kubernetes/ACA HTTP probes in production orchestration.

Build-stage `npm ci --ignore-scripts` + `npm rebuild esbuild` reduces supply-chain script execution during web image builds.

---

### Build context and `.dockerignore`

**Fix applied:** Expanded `.dockerignore` to exclude:

- `tests/`, `.github/`, compose files, agent output
- Secrets patterns (`.env*`, `secrets.json`)
- Build artifacts (`bin/`, `obj/`, `node_modules/`, `dist/`)

**Decision:** Repository root remains the build context (required for `src/` + `frontend/` + `infra/docker/` paths) but context is trimmed to production-necessary files only.

---

### Health checks

| Service | Probe | Notes |
|---------|-------|-------|
| API | `GET /health/live` (Docker `HEALTHCHECK`) | Liveness — process up |
| API | `GET /health/ready` (Compose `depends_on`) | Readiness — DB + storage |
| Web | `GET /health` | nginx-only; independent of API |
| Postgres | `pg_isready` | Standard |
| Azurite | TCP connect :10000 | Dev/test stacks |

**Decision:** Keep both Docker image `HEALTHCHECK` and Compose health conditions for local/CI reliability.

---

## Fixes applied (summary)

| File | Change |
|------|--------|
| `ContentForge.Api.csproj` | Exclude Development/Testing appsettings from publish |
| `infra/docker/api/Dockerfile` | Remove dev configs after publish; `icu-libs` only; clear apk cache |
| `infra/docker/web/Dockerfile` | Pin Node Alpine; `npm ci --ignore-scripts`; chown static assets; strip `node_modules` |
| `infra/docker/web/nginx.conf.template` | `server_tokens off`; security headers |
| `.dockerignore` | Exclude tests, CI, compose, secrets from build context |
| `docker-compose.prod.yml` | `read_only`, `tmpfs`, `no-new-privileges`, internal API `expose` |

---

## Verification (2026-09-01)

```bash
# Clean production image build
docker build --no-cache -f infra/docker/api/Dockerfile -t contentforge-api:local .
docker build --no-cache -f infra/docker/web/Dockerfile -t contentforge-web:local .

# Publish output check
dotnet publish src/ContentForge.Api/ContentForge.Api.csproj -c Release -o ./artifacts/publish
# → no appsettings.Development.json or appsettings.Testing.json

# CI parity
dotnet build && dotnet test && dotnet format --verify-no-changes
cd frontend/contentforge-web && npm ci && npm audit --audit-level=moderate && npm run lint && npm run test && npm run build
```

---

## Residual accepted risks

| Item | Mitigation |
|------|------------|
| `wget` in API runtime | Required for Docker health check; use orchestrator probes in K8s/ACA |
| Local dev Compose default passwords | Documented dev-only; not used in prod compose |
| Azurite well-known emulator key | Dev/test only; never used in production images |
| API application binaries writable by `appuser` | Required for .NET runtime; mitigated by `read_only` root + media volume in prod compose |

---

## Ongoing maintenance

1. Rebuild and scan images when base tags are updated (`global.json`, Node LTS, nginx patch releases).
2. Confirm `dotnet publish` never regresses Development/Testing config inclusion when adding new environment files.
3. Run `docker build --no-cache` before release candidates and smoke-test `/health/live`, `/health/ready`, and login.
