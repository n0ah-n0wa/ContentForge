# ContentForge — Implementation Plan

**Version:** 1.1  
**Status:** Complete (retrospective)  
**Source of Truth (requirements):** [SPECIFICATIONS.md](../SPECIFICATIONS.md)  
**As-built architecture:** [ARCHITECTURE.md](./ARCHITECTURE.md)

This document records the phased delivery of ContentForge. All phases below are **done** relative to the current repository. Use it as historical context and a checklist of delivered capability—not as an open backlog.

---

## Overview

| Stage | Phases | Outcome |
|-------|--------|---------|
| **Foundation** | 0–2 | Solution, persistence, auth |
| **Core CMS** | 3–7 | Types, entries, lifecycle, versioning, media, schema evolution |
| **API Surface** | 8–10 | Public API, admin search, audit |
| **Frontend** | 11–13 | Admin SPA |
| **Production** | 14–16 | Hardening, CI/CD, Azure |

```text
0 → 1 → 2 → 3 → 4 → 5 → 8 → 11 → 12 → 15 → 16
         └→ 6 (media) parallel after storage port
         └→ 7 (schema evolution) with types/entries
         └→ 9–10 after content APIs
         └→ 13–14 with frontend/ops
```

---

## Phase status

### Phase 0 — Solution scaffolding — **Done**

- .NET 8 solution: Domain, Application, Infrastructure, Api (+ later SqlServer)
- Unit, Integration, Architecture test projects
- Vue 3 + TypeScript + Vite scaffold under `frontend/contentforge-web`
- `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`
- Baseline architecture tests; ADRs 001–002

### Phase 1 — Persistence & local infrastructure — **Done**

- EF Core `AppDbContext`, PostgreSQL provider, migrations
- `docker-compose.yml` / `docker-compose.test.yml` (Postgres + Azurite)
- `/health/live`, `/health/ready`
- Development seed/initializer; `IFileStorage` port

### Phase 2 — Authentication & authorization — **Done**

- ASP.NET Identity + JWT access/refresh ([ADR-004](./decisions/ADR-004-authentication-strategy.md))
- Roles Administrator / Editor / Author / Viewer and permission policies
- Auth API: login, logout, refresh, forgot/reset password, me

### Phase 3 — Content types — **Done**

- Dynamic schema ([ADR-003](./decisions/ADR-003-dynamic-content-schema.md))
- Field CRUD, deactivate type, rename/remove field with destructive confirmations

### Phase 4 — Content entries & lifecycle — **Done**

- Draft/review/publish/unpublish/archive/restore flows
- Optimistic concurrency
- Publishing snapshot model ([ADR-007](./decisions/ADR-007-publishing-architecture.md))

### Phase 5 — Versioning — **Done**

- Version list/get/compare/restore ([ADR-006](./decisions/ADR-006-content-versioning.md))

### Phase 6 — Media — **Done**

- Upload/list/update/delete; `/media-files` serving
- Local + Azure/Azurite storage ([ADR-005](./decisions/ADR-005-media-storage.md))

### Phase 7 — Relations & schema evolution — **Done**

- Relation / relation-multiple fields; destructive schema change guards

### Phase 8 — Public API — **Done**

- `/api/v1/public/{contentTypeSlug}` and `/{slug}`; published-only reads; caching options

### Phase 9 — Admin search & query — **Done**

- Filters, sort, pagination, keyword search via `IContentSearchService` ([ADR-008](./decisions/ADR-008-search-abstraction.md))

### Phase 10 — Audit logging — **Done**

- Immutable audit records; admin query API and UI

### Phase 11 — Frontend foundation — **Done**

- Router, Pinia (auth/notifications/UI), API client, layout, login, guards

### Phase 12 — Frontend CMS features — **Done**

- Content types, entries, lifecycle actions, media library, versioning UX

### Phase 13 — Frontend admin & UX — **Done**

- Users, roles, audit, dashboard, a11y foundations, notifications

### Phase 14 — Observability & security hardening — **Done**

- Rate limiting, sanitization, health, App Insights wiring, security reviews under `docs/architecture/`

### Phase 15 — CI/CD — **Done**

- `.github/workflows/ci.yml` — backend, frontend, infra smoke, E2E
- Migration validation scripts ([ADR-009](./decisions/ADR-009-cicd-and-migrations.md))

### Phase 16 — Azure deployment & advanced features — **Done**

- Bicep environments; `.github/workflows/deploy.yml` (OIDC)
- Azure SQL migrations; scheduled publishing ([ADR-011](./decisions/ADR-011-scheduled-publishing.md))
- Hosting topology ([ADR-010](./decisions/ADR-010-azure-hosting-topology.md))
- Preview tokens; production reviews (backend/frontend)

---

## ADR index

| ADR | Topic | Status |
|-----|-------|--------|
| [ADR-001](./decisions/ADR-001-clean-architecture.md) | Clean Architecture | Accepted |
| [ADR-002](./decisions/ADR-002-database-strategy.md) | PostgreSQL local / Azure SQL prod | Accepted |
| [ADR-003](./decisions/ADR-003-dynamic-content-schema.md) | Dynamic content schema | Accepted |
| [ADR-004](./decisions/ADR-004-authentication-strategy.md) | JWT + Identity | Accepted |
| [ADR-005](./decisions/ADR-005-media-storage.md) | Media storage abstraction | Accepted |
| [ADR-006](./decisions/ADR-006-content-versioning.md) | Content versioning | Accepted |
| [ADR-007](./decisions/ADR-007-publishing-architecture.md) | Draft vs published | Accepted |
| [ADR-008](./decisions/ADR-008-search-abstraction.md) | Search abstraction | Accepted |
| [ADR-009](./decisions/ADR-009-cicd-and-migrations.md) | CI/CD and migrations | Accepted |
| [ADR-010](./decisions/ADR-010-azure-hosting-topology.md) | Azure hosting | Accepted |
| [ADR-011](./decisions/ADR-011-scheduled-publishing.md) | Scheduled publishing | Accepted |

---

## Verification baseline

Current verification expectations match CI ([operations/ci.md](./operations/ci.md)):

```bash
dotnet restore
dotnet format ContentForge.sln --verify-no-changes
dotnet build ContentForge.sln --configuration Release
dotnet test --configuration Release --no-build

cd frontend/contentforge-web
npm ci
npm run format:check && npm run lint && npm run typecheck && npm run test && npm run build:vite

# optional E2E (from repository root for compose; frontend dir for Playwright)
# docker compose -f docker-compose.e2e.yml up -d --build
# npm run e2e:install && npm run test:e2e
```

Production readiness narratives: [architecture/final-backend-review.md](./architecture/final-backend-review.md), [architecture/final-frontend-review.md](./architecture/final-frontend-review.md).

---

## Out of scope (still non-goals)

Per SPEC §3 — not planned as product features: page builder, multi-tenant SaaS, real-time collab, full DAM, marketing automation. Hardening follow-ons (private endpoints, Front Door/WAF) are optional ops improvements, not missing CMS phases.
