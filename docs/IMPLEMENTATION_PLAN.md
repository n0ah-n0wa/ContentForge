# ContentForge — Implementation Plan

**Version:** 1.0  
**Status:** Planning  
**Source of Truth:** [SPECIFICATIONS.md](../SPECIFICATIONS.md)

This document defines the phased implementation roadmap for ContentForge. Phases are ordered by dependency; later phases assume earlier phases are complete and verified.

---

## Overview

ContentForge is implemented incrementally in **17 phases** grouped into five stages:

| Stage | Phases | Outcome |
|-------|--------|---------|
| **Foundation** | 0–2 | Runnable solution, persistence, auth |
| **Core CMS** | 3–7 | Content types, entries, lifecycle, versioning, media |
| **API Surface** | 8–10 | Public API, admin search, audit |
| **Frontend** | 11–13 | Admin dashboard and editors |
| **Production** | 14–16 | Observability, CI/CD, Azure deployment |

Each phase ends with verification (build, tests, relevant checks) per [DEVELOPMENT_RULES.md](./DEVELOPMENT_RULES.md).

---

## Dependency Graph

```text
Phase 0 (Scaffolding)
    │
    ▼
Phase 1 (Persistence & Docker infra)
    │
    ├──────────────────┐
    ▼                  ▼
Phase 2 (Auth)    Phase 6 (Media) ──────────────┐
    │                  │                         │
    ▼                  │                         │
Phase 3 (Content Types)◄────────────────────────┘
    │
    ▼
Phase 4 (Content Entries & Lifecycle)
    │
    ├──────────────────┐
    ▼                  ▼
Phase 5 (Versioning)  Phase 7 (Relations & Schema Evolution)
    │                  │
    └────────┬─────────┘
             ▼
Phase 8 (Public API)
             │
             ▼
Phase 9 (Admin Search & Query)
             │
             ▼
Phase 10 (Audit Logging)
             │
             ▼
Phase 11 (Frontend Foundation)
             │
             ▼
Phase 12 (Frontend CMS Features)
             │
             ▼
Phase 13 (Frontend Admin & UX)
             │
             ▼
Phase 14 (Observability & Security Hardening)
             │
             ▼
Phase 15 (CI/CD Pipeline)
             │
             ▼
Phase 16 (Azure Deployment & Advanced Features)
```

**Critical path:** 0 → 1 → 2 → 3 → 4 → 5 → 8 → 11 → 12 → 15 → 16

**Parallelizable after Phase 2:** Phase 6 (Media) can proceed in parallel with Phase 3–5 if storage abstraction is defined in Phase 1.

**Parallelizable after Phase 10:** Frontend phases 11–13 require backend APIs but can overlap partially (e.g., auth UI after Phase 2 with mocked endpoints, full integration after Phase 10).

---

## Phase 0 — Solution Scaffolding

**Objective:** Establish repository structure, solution layout, and quality tooling without business features.

### Deliverables

- .NET 8 solution with projects:
  - `ContentForge.Api`
  - `ContentForge.Application`
  - `ContentForge.Domain`
  - `ContentForge.Infrastructure`
- Test projects:
  - `ContentForge.UnitTests`
  - `ContentForge.IntegrationTests`
  - `ContentForge.ArchitectureTests`
- `frontend/contentforge-web` (Vue 3 + TypeScript + Vite scaffold)
- `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`
- Architecture test baseline (forbidden dependency rules)
- `README.md` skeleton with setup placeholders
- Initial ADRs in `docs/decisions/` (Clean Architecture, database strategy)

### Dependencies

None.

### Exit Criteria

- [ ] Solution builds with zero warnings (nullable enabled)
- [ ] Architecture tests pass (empty or baseline rules)
- [ ] Frontend scaffold builds
- [ ] `dotnet format --verify-no-changes` passes

---

## Phase 1 — Persistence Foundation & Local Infrastructure

**Objective:** Database connectivity, EF Core setup, Docker Compose for local development, health checks.

### Deliverables

- EF Core DbContext and initial migration (system tables skeleton)
- Entity configurations for: Users, Roles, Permissions (schema only)
- PostgreSQL provider for Development/Test
- `docker-compose.yml` with PostgreSQL (+ Azurite placeholder service)
- `docker-compose.test.yml` for integration tests
- Health endpoints: `/health/live`, `/health/ready`
- Configuration model (Development, Test, Staging, Production)
- `IFileStorage` interface in Domain/Application (no implementation yet)
- Seed data infrastructure (disabled in Production)

### Dependencies

Phase 0.

### Exit Criteria

- [ ] Migrations apply cleanly against PostgreSQL
- [ ] Readiness check verifies database connectivity
- [ ] `docker compose up` starts required infrastructure
- [ ] Integration test proves DB round-trip

---

## Phase 2 — Authentication & Authorization

**Objective:** Identity, JWT auth, permission-based authorization, user/role management APIs.

### Deliverables

- ASP.NET Core Identity integration
- JWT access token issuance and validation
- Refresh token strategy (if implemented — document in ADR)
- Password policies, account lockout/throttling
- Permission model: roles → permissions mapping
- Initial roles: Administrator, Editor, Author, Viewer
- Authorization handlers/policies (centralized, testable)
- Admin API endpoints:
  - `/api/v1/auth/*`
  - `/api/v1/users/*`
  - `/api/v1/roles/*`
- RFC 7807 error responses for auth failures
- Rate limiting on login and password reset endpoints
- Unit tests: permission logic
- Integration tests: auth, authorization, disabled accounts, invalid JWTs

### Dependencies

Phase 1.

### Exit Criteria

- [ ] Login/logout flows work
- [ ] Protected endpoints reject unauthorized/forbidden requests
- [ ] All four roles enforce correct permissions
- [ ] Security integration tests pass

---

## Phase 3 — Content Type System

**Objective:** Dynamic content type definitions with field schemas and validation rules.

### Deliverables

- Domain entities: `ContentType`, `ContentTypeField`
- Supported field types (Text, LongText, RichText, Integer, Decimal, Boolean, Date, DateTime, Media, MediaMultiple, Relation, RelationMultiple, Select, MultiSelect, Json)
- Field configuration validation (required, min/max, pattern, options, relationTarget, etc.)
- Content type constraints: unique names/slugs, no duplicate fields, safe schema evolution guards
- Admin API: `/api/v1/content-types/*`
- Application validators (FluentValidation)
- Dynamic schema validation service
- Demo seed content types: Article, Page, Author, Category
- Unit tests: schema validation, constraint enforcement

### Dependencies

Phase 2 (CreatedBy/UpdatedBy requires authenticated users).

### Exit Criteria

- [ ] CRUD for content types with authorization
- [ ] Field definitions validated per type
- [ ] Content types with dependent entries cannot be unsafely deleted
- [ ] Demo content types seeded in Development

---

## Phase 4 — Content Entries & Lifecycle

**Objective:** Content CRUD, lifecycle state machine, draft vs published separation.

### Deliverables

- Domain entities: `ContentEntry` with JSON `Data`, system metadata
- Lifecycle states: DRAFT, IN_REVIEW, PUBLISHED, ARCHIVED, UNPUBLISHED
- Explicit transition rules with domain errors on invalid transitions
- Draft data separate from published representation
- Slug normalization, validation, uniqueness per content type
- Optimistic concurrency token (`RowVersion`) on entry entity
- Admin API: `/api/v1/content/*` (CRUD, transitions, submit, publish, unpublish, archive)
- Publish operation executes full business rules (not a status-only update)
- Structured validation errors (422) against content type schema
- Pagination, sorting, filtering (whitelist) on list endpoints
- Unit tests: lifecycle transitions, slug rules, publish rules
- Integration tests: CRUD, transitions, concurrency (409)

### Dependencies

Phase 3.

### Exit Criteria

- [ ] Full lifecycle workflow operable via API
- [ ] Draft edits do not alter published representation
- [ ] Publish creates version and audit hook points (audit in Phase 10)
- [ ] Concurrency conflict returns 409 with actionable response

---

## Phase 5 — Content Versioning

**Objective:** Immutable version history, compare, restore-as-new-version.

### Deliverables

- Domain entity: `ContentVersion` (Snapshot, VersionNumber, ChangeSummary)
- Monotonically increasing version numbers per entry
- Version listing, retrieval, comparison
- Restore creates new version (historical versions immutable)
- Admin API: version endpoints under `/api/v1/content/{type}/{id}/versions`
- Transaction boundaries for publish and restore
- Unit tests: versioning rules, restore behavior
- Integration tests: version history after edits and publish

### Dependencies

Phase 4.

### Exit Criteria

- [ ] Every meaningful mutation creates a version
- [ ] Restore produces new version without mutating history
- [ ] Version compare returns meaningful diff structure

---

## Phase 6 — Media Library

**Objective:** Media upload, metadata management, secure storage abstraction.

### Deliverables

- Domain entity: `Media` (metadata only; binaries in object storage)
- `IFileStorage` implementations:
  - Local filesystem (Development)
  - Azurite / Azure Blob compatible (Development/Test)
  - Azure Blob Storage (Production configuration)
- Upload validation: MIME, extension, size, filename sanitization, path traversal prevention
- Storage keys generated by system (never user-provided paths)
- Admin API: `/api/v1/media/*`
- Rate limiting on upload
- Unit tests: validation rules, path safety
- Integration tests: upload, retrieve metadata, delete

### Dependencies

Phase 1 (storage abstraction), Phase 2 (authorization).

Can run in parallel with Phases 3–5 once Phase 2 is complete.

### Exit Criteria

- [ ] Media binaries stored outside relational database
- [ ] Upload/delete authorized by permissions
- [ ] Storage abstraction swappable via configuration

---

## Phase 7 — Relations & Schema Evolution

**Objective:** Inter-entry relationships and safe content type schema changes.

### Deliverables

- Relation field support: one-to-one, many-to-one, one-to-many, many-to-many
- Relation validation (referenced entity existence)
- Business rules for deleted/archived referenced entities
- Content type schema evolution workflow (add/remove/rename field, change validation)
- Destructive schema changes require explicit confirmation
- Additional persistence tables for relations as needed
- Soft deletion support for content entries where appropriate
- Unit tests: relation validation, schema evolution guards

### Dependencies

Phase 4, Phase 3 (relation field definitions).

Phase 6 required for Media relation fields to resolve completely.

### Exit Criteria

- [ ] Article → Author, Category, Tags relations work end-to-end
- [ ] Schema changes do not silently corrupt existing content
- [ ] Soft-deleted content excluded from normal queries but recoverable

---

## Phase 8 — Public API

**Objective:** Headless public content consumption API exposing published content only.

### Deliverables

- Public endpoints: `/api/v1/public/{contentType}`, `/api/v1/public/{contentType}/{slug}`
- Stable public response DTOs (distinct from admin DTOs)
- Published content only; no drafts, audit data, or internal user info
- Pagination and appropriate filtering
- Rate limiting on public endpoints
- Optional output/data caching with invalidation on publish
- Integration tests: public access, draft isolation, pagination

### Dependencies

Phase 4 (published representation), Phase 5 (optional version metadata exclusion).

### Exit Criteria

- [ ] Public API returns only published content
- [ ] Slug-based retrieval works per content type
- [ ] Public DTOs documented in OpenAPI

---

## Phase 9 — Administrative Search & Query

**Objective:** Replaceable search implementation for admin content discovery.

### Deliverables

- `IContentSearchService` abstraction (Domain/Application)
- Initial implementation: EF Core / PostgreSQL keyword and field filtering
- Admin search: full-text-like keyword, field/status/type/author/date filters
- Controlled sorting (explicit whitelist)
- Consistent pagination response shape
- Rejection of unsupported filters (400, not silent ignore)
- Integration tests: search, filter combinations, pagination limits

### Dependencies

Phase 4, Phase 7 (relations may affect search filters).

### Exit Criteria

- [ ] Admin content list/search meets spec requirements
- [ ] Search implementation swappable without domain changes
- [ ] Max page size enforced server-side

---

## Phase 10 — Audit Logging

**Objective:** Immutable audit trail for security and content operations.

### Deliverables

- Domain entity: `AuditLog` (immutable)
- Audit event types per specification (UserCreated, ContentPublished, MediaUploaded, etc.)
- Audit service integrated into auth and content workflows
- Admin API: `/api/v1/audit/*` (read-only, permission-gated)
- IP address and user agent capture
- Integration tests: events emitted on key operations

### Dependencies

Phase 2 (auth events), Phase 4 (content events), Phase 6 (media events).

Note: Audit *hooks* should be planned in Phases 2, 4, 6; Phase 10 completes the subsystem and backfills any missing events.

### Exit Criteria

- [ ] All specified auditable events recorded
- [ ] Audit entries immutable
- [ ] Only authorized users can read audit log

---

## Phase 11 — Frontend Foundation

**Objective:** Vue admin shell with auth, routing, API layer, and permission guards.

### Deliverables

- Strict TypeScript configuration
- Pinia stores: `authStore` (initial)
- API layer: `api/auth.ts`, centralized headers and error handling
- Vue Router with auth guards
- Login page, layout shell, access-denied state
- RFC 7807 error display
- Route structure per specification (skeleton routes)
- Frontend unit tests: auth store, route guards
- Component test baseline

### Dependencies

Phase 2 (auth API).

### Exit Criteria

- [ ] Unauthenticated users redirected to `/login`
- [ ] Unauthorized users see access-denied state
- [ ] Token handling secure (no secrets in source)
- [ ] `npm run build`, `npm run test`, `npm run lint` pass

---

## Phase 12 — Frontend CMS Features

**Objective:** Dynamic content editor, content types UI, media library, versioning UI.

### Deliverables

- Pinia stores: `contentStore`, `contentTypeStore`, `mediaStore`
- API modules: `content.ts`, `contentTypes.ts`, `media.ts`
- Dynamic form renderer mapped to field types
- Content list, create, edit, version history views
- Content type management UI
- Media library: grid/list, upload with progress, metadata edit, selection
- Concurrency conflict UX (409 handling)
- Draft save, status display, permission-gated publish actions
- Rich text sanitization on render
- WCAG 2.1 AA baseline (keyboard, labels, focus, form errors)
- Frontend tests: dynamic forms, validation display, stores

### Dependencies

Phases 3–6, 8–9 (APIs), Phase 11.

### Exit Criteria

- [ ] Full content workflow operable from UI
- [ ] Dynamic editor renders all supported field types
- [ ] Media selectable from content editor
- [ ] Unsaved changes warning where practical

---

## Phase 13 — Frontend Administration & Dashboard

**Objective:** Users, roles, audit log, settings, profile, dashboard.

### Deliverables

- Pinia stores: `userStore`, `auditStore`
- API modules: `users.ts`, `audit.ts`
- Dashboard with backend-sourced statistics
- User management UI
- Roles display/management (per permissions)
- Audit log viewer
- Settings and profile pages
- Responsive layout (desktop, laptop, tablet)
- Frontend tests: permission-based UI visibility

### Dependencies

Phase 2, Phase 10, Phase 11, Phase 12 (partial).

### Exit Criteria

- [ ] Dashboard shows operational statistics from API
- [ ] Role-appropriate UI restrictions enforced
- [ ] All specified routes functional

---

## Phase 14 — Observability, Security & Cross-Cutting Concerns

**Objective:** Production diagnostics, logging, correlation, remaining security controls.

### Deliverables

- Structured logging (Serilog or built-in with JSON formatter)
- Correlation/trace ID middleware (accept, generate, propagate to logs and errors)
- Application Insights integration (Production/Staging)
- Request, exception, dependency telemetry
- Secure headers middleware
- CORS allowlist configuration
- Secrets via User Secrets (.env local), Azure Key Vault / App Service settings (Production)
- Password reset workflow
- Content preview (authenticated, short-lived scoped tokens — not on public API)
- Background job abstraction (interface only; no full job system unless needed)
- API idempotency for publish/delete where practical
- Cancellation token propagation audit
- Dependency vulnerability scanning in CI

### Dependencies

Phases 1–13 (integrates across system).

### Exit Criteria

- [ ] Logs structured with traceId, no secrets logged
- [ ] Application Insights receives telemetry in Staging
- [ ] CORS and secure headers configured per environment
- [ ] Preview does not expose drafts on public API

---

## Phase 15 — CI/CD Pipeline

**Objective:** GitHub Actions for validation, testing, building, and deployment automation.

### Deliverables

- CI workflow: restore, build, format/lint, unit tests, integration tests, architecture tests, frontend tests, frontend build, backend publish
- CD workflow: artifact build, container image, deployment, health check, verification
- `docker-compose.test.yml` integration in CI
- Production Dockerfile (multi-stage, non-root)
- PR merge blocked on failed checks
- Staging deployment with production-like configuration
- Documented migration process (no uncontrolled auto-migrate in Production)

### Dependencies

Phases 0–14 (tests and build targets must exist).

### Exit Criteria

- [ ] CI passes on clean checkout
- [ ] Docker image builds successfully
- [ ] CD deploys to Staging with health check verification

---

## Phase 16 — Azure Deployment & Production Features

**Objective:** Azure production topology, scheduled publishing, backup/recovery documentation, E2E tests.

### Deliverables

- Azure App Service deployment (backend + frontend static hosting strategy per ADR)
- Azure SQL Database (Production) with EF Core migration strategy
- Azure Blob Storage (Production media)
- Managed identity for Azure service access
- Deployment slots (where appropriate)
- Scheduled publishing (`publishAt`, `unpublishAt`) with idempotent execution
- E2E tests: auth, content workflow, authorization, media (Playwright or Cypress)
- Operations documentation:
  - Database backup/recovery
  - Media recovery
  - Disaster recovery procedure
  - Secret recovery
- Example API usage documentation in `docs/api/`
- OpenAPI suitable for client generation
- Production readiness checklist (SPEC §105)

### Dependencies

Phase 15.

### Exit Criteria

- [ ] Azure deployment succeeds with health checks passing
- [ ] Scheduled publish/unpublish works across process restarts
- [ ] Critical E2E scenarios pass
- [ ] Backup/recovery documented in `docs/operations/`
- [ ] All Production Readiness checklist items satisfied

---

## Testing Strategy Across Phases

Tests are not deferred to a single phase. Each phase adds tests for its scope:

| Phase | Unit | Integration | Architecture | Frontend | E2E |
|-------|------|-------------|--------------|----------|-----|
| 0 | — | — | ✓ baseline | — | — |
| 1 | — | ✓ DB | ✓ | — | — |
| 2 | ✓ auth | ✓ auth | ✓ | — | — |
| 3–7 | ✓ domain | ✓ API | ✓ | — | — |
| 8–10 | — | ✓ API | — | — | — |
| 11–13 | — | — | — | ✓ | — |
| 16 | — | ✓ security | — | — | ✓ critical paths |

---

## ADR Schedule

Architecture Decision Records should be created when the decision is made, not retroactively:

| ADR | Phase | Topic |
|-----|-------|-------|
| ADR-001 | 0 | Clean Architecture |
| ADR-002 | 1 | PostgreSQL locally / Azure SQL in production |
| ADR-003 | 3 | Dynamic content schema representation |
| ADR-004 | 2 | Authentication strategy (JWT + Identity) |
| ADR-005 | 6 | Media storage abstraction |
| ADR-006 | 5 | Content versioning strategy |
| ADR-007 | 4 | Publishing architecture (draft vs published) |
| ADR-008 | 9 | Search abstraction |
| ADR-009 | 15 | CI/CD and migration deployment |
| ADR-010 | 16 | Azure hosting topology (App Service layout) |
| ADR-011 | 16 | Scheduled publishing mechanism |

---

## Risk Register (Planning)

| Risk | Mitigation Phase |
|------|------------------|
| Draft/published dual representation complexity | Phase 4 ADR-007; extensive integration tests |
| Dynamic schema validation performance | Phase 3 indexed queries; Phase 9 search abstraction |
| PostgreSQL vs Azure SQL dialect differences | Phase 1 isolate DB-specific code; CI test both where feasible |
| Frontend dynamic editor complexity | Phase 12 incremental field type rollout |
| Scheduled publishing reliability | Phase 16 idempotent job design; ADR-011 |

---

## Definition of Phase Completion

A phase is complete when:

1. All deliverables are implemented.
2. Phase exit criteria are satisfied.
3. Relevant tests pass (`dotnet test`, frontend tests as applicable).
4. No regressions in prior phases.
5. Documentation updated (API docs, ADRs, README sections as needed).
6. AI agent verification commands run per [DEVELOPMENT_RULES.md](./DEVELOPMENT_RULES.md).

See also SPECIFICATIONS.md §106 (Definition of Done).
