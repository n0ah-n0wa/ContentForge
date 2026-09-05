# ContentForge — Specification Compliance Audit

**Authority:** [SPECIFICATIONS.md](../../SPECIFICATIONS.md)  
**Date:** 2026-09-05  
**Purpose:** Detect missing requirements before release. This audit does **not** redefine the specification and does **not** certify production readiness.

**Status legend**

| Status | Meaning |
|--------|---------|
| **IMPLEMENTED** | Requirement is met in code (and usually tests/docs); evidence cited |
| **PARTIALLY IMPLEMENTED** | Material part exists; SPEC-required aspect still missing |
| **NOT IMPLEMENTED** | Required capability absent |
| **NOT APPLICABLE** | Non-goal, process rule, illustrative example, or positioning text |

---

## Executive summary

| Status | Count (significant items below) |
|--------|--------------------------------:|
| IMPLEMENTED | Majority of core CMS / API / architecture / testing / Docker / CI |
| PARTIALLY IMPLEMENTED | Settings/Profile UI, password-reset UI, schedule UI, soft-delete restore API, seed richness, authz DB vs code matrix, Azure live proof, a11y completeness, §105 certification, §116 soft-delete/schedule rows |
| NOT IMPLEMENTED | Password-reset SPA; Settings/Profile functionality; private endpoints (if treated as Azure hardening MUST beyond residual acceptance); §105 as completed checklist |
| NOT APPLICABLE | §3 non-goals; AI/git process §§107–115; portfolio §117 |

**Highest-priority gaps before release**

1. Settings and Profile are placeholders (§33–§34) while listed as required admin sections.  
2. Secure password-reset **workflow** exists in API; **no admin UI** (§31, §33).  
3. Scheduled publishing is backend-complete; **no SPA schedule controls** (§86, §116).  
4. Soft deletion is stored and filtered; **recover/restore soft-deleted content via API/UI is missing** (§89).  
5. Development seed is admin-only; SPEC **should** seed roles/users/samples (§99–§100).  
6. §105 production-readiness checklist remains unchecked; see [PRODUCTION_READINESS_REPORT.md](./PRODUCTION_READINESS_REPORT.md).

---

## Checklist by SPEC section

### §1–§3 Project overview, goals, non-goals

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 1.2 | Headless CMS: .NET 8 backend, Vue admin, RDBMS, media, authz, lifecycle, versioning, audit, search, OpenAPI, tests, observability, Docker, CI/CD, Azure | **IMPLEMENTED** | `src/`, `frontend/contentforge-web/`, `tests/`, `infra/`, `.github/workflows/`, `README.md` |
| 2.1 | Primary engineering goals (C#/.NET/EF/REST/auth/Vue/Docker/Azure/testing/…) | **IMPLEMENTED** | Same as above |
| 2.2 | Secondary goals (maintainability, migrations, concurrency, diagnostics, …) | **IMPLEMENTED** | EF migrations, concurrency tokens, Problem Details, App Insights wiring, ADRs |
| 3 | Non-goals (no multi-tenant SaaS, page builder, full DAM, marketing automation, …) | **NOT APPLICABLE** | Correctly absent; documented in SPEC and `IMPLEMENTATION_PLAN.md` |

### §4–§6 Architecture, technology stack, repository structure

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 4 | Clean Architecture; Domain free of ASP.NET/EF/Azure/Vue | **IMPLEMENTED** | `ContentForge.Domain`; `tests/ContentForge.ArchitectureTests/LayerDependencyTests.cs`; ADR-001 |
| 5.1–5.6 | Tech stack (.NET 8, Identity+JWT, FluentValidation, Vue3+TS+Vite+Pinia, PG/Azure SQL, Blob, Docker, GH Actions, App Insights) | **IMPLEMENTED** | `Directory.Packages.props`, `package.json`, compose, Bicep, workflows |
| 6 | Repository structure (src/frontend/tests/docs/infra/workflows) | **IMPLEMENTED** | Tree matches; additional `Infrastructure.SqlServer` is compatible extension for dual providers (ADR-002/009) |

### §7–§8 Domain: users, roles, permissions

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 7.1 | User model (id, email, display name, role, active, timestamps; Identity OK) | **IMPLEMENTED** | `ContentForgeUser`, Identity EF store |
| 8 | Roles Administrator / Editor / Author / Viewer with listed permission intents | **IMPLEMENTED** | `RoleName`, `DefaultRoleDefinitions`, seeded roles |
| 8 | Permission-based authorization (not role-name checks alone) | **PARTIALLY IMPLEMENTED** | Policies + `PermissionAuthorizationHandler` enforce permission strings. **Gap:** runtime matrix is **code** (`DefaultRoleDefinitions` / JWT claims). DB `RolePermissions` is seeded from code and is **not** an independent runtime source of truth. SPEC implies permission catalog; it does not require DB-editable matrix, but operators cannot change authz via DB alone. |
| 8 | Administrator “role management” / “system configuration” | **PARTIALLY IMPLEMENTED** | Roles list API/UI (`RolesController`, `RoleListView.vue`). **Gap:** no role CRUD or system-configuration surface beyond listing permissions. |

### §9 Content types

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 9 | Dynamic content types | **IMPLEMENTED** | Domain `ContentType`, Application commands, API, Vue editors |
| 9.2 | Field types Text, LongText, RichText, Integer, Decimal, Boolean, Date, DateTime, Media, MediaMultiple, Relation, RelationMultiple, Select, MultiSelect, Json | **IMPLEMENTED** | `FieldType` enum; FE `fieldTypeDefinitions.ts`; integration tests for field types |
| 9.3 | Constraints (unique names/slugs, validation, safe delete) | **IMPLEMENTED** | Domain validation; destructive confirmations |

### §10–§16 Content entries, lifecycle, draft/published, concurrency, slugs, validation

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 10 | Content entries with metadata + field data | **IMPLEMENTED** | `ContentEntry`, draft JSON, API/UI |
| 11 | Lifecycle Draft → InReview → Published → Unpublished/Archived with explicit transitions | **IMPLEMENTED** | `ContentLifecycle`, lifecycle endpoints, FE actions, E2E `content-workflow.spec.ts` |
| 12 | Separate draft vs published; publish creates version; public sees published only; audit | **IMPLEMENTED** | `DraftDataJson` / `PublishedSnapshotJson`; public API; audit on publish; ADR-007 |
| 13 | Version list/get/compare/restore (restore creates new version) | **IMPLEMENTED** | Version endpoints; FE history panel; API/E2E tests |
| 14 | Optimistic concurrency → conflict (409) | **IMPLEMENTED** | `concurrencyToken`; Problem Details 409; FE conflict panel; concurrency E2E |
| 15 | Slugs unique within type; normalized | **IMPLEMENTED** | `Slug` VO; uniqueness rules |
| 16 | Dynamic validation + structured errors | **IMPLEMENTED** | `ContentDataValidator`; 422 Problem Details |

### §17 Relations

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 17 | Relation cardinalities; validate referenced entries exist | **IMPLEMENTED** | `RelationCardinality`; relation validation in content validator; FE relation fields |

### §18–§19 Media

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 18 | Media library metadata + object storage (not DB blobs) | **IMPLEMENTED** | `MediaAsset`, `IFileStorage`, Media API/UI |
| 19 | MIME/extension/size/filename/path safety | **IMPLEMENTED** | `MediaUploadRules`, `MediaContentInspector`; media security integration tests |

### §20–§23 Search, pagination, sorting, filtering

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 20 | Admin search with keyword + filters + sort + page; replaceable abstraction | **IMPLEMENTED** | `IContentSearchService`, `EfContentSearchService`, `PortableSearch` (contains/ILIKE — not a separate FTS engine; ADR-008) |
| 21 | Pagination (`page`, `pageSize`, totals) | **IMPLEMENTED** | `Pagination` helpers; list endpoints |
| 22 | Whitelisted sort fields | **IMPLEMENTED** | `SortRequest.EnsureAllowed` |
| 23 | Whitelisted filters; reject unsupported | **IMPLEMENTED** | `QueryBinding.EnsureAllowedQueryParameters` |

### §24–§28 Public/admin API, versioning, REST, errors

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 24 | Public published-only API; pagination/filtering; no drafts/users/audit | **IMPLEMENTED** | `PublicContentController`; rate limited; tests |
| 25 | Admin API surface (auth, users, roles, content-types, content, media, audit) | **IMPLEMENTED** | Controllers under `/api/v1/...` |
| 26 | `/api/v1` versioning | **IMPLEMENTED** | `ApiConstants.VersionPrefix` |
| 27 | REST status semantics | **IMPLEMENTED** | Controllers + Problem Details |
| 28 | RFC 7807 Problem Details; no production stack traces | **IMPLEMENTED** | Exception middleware / Problem Details factory |

### §29–§31 Authentication & account security

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 29 | ASP.NET Identity + JWT; login/logout; password hashing; secure credentials | **IMPLEMENTED** | `AuthController`, Identity, JWT bearer |
| 29 | Refresh tokens if implemented | **IMPLEMENTED** | Refresh API + FE session refresh |
| 30 | Server-side permission checks; centralized/testable | **IMPLEMENTED** | Policies, `ApplicationGuard`, authorization tests |
| 31 | Lockout / login protection | **IMPLEMENTED** | Identity lockout; auth rate limits |
| 31 | Secure password reset workflow | **PARTIALLY IMPLEMENTED** | **API:** forgot/reset + notifier (Logging non-prod / Smtp Production). Tests: `AuthIntegrationTests`. **Gap:** **no SPA UI** for forgot/reset; operators must use API/email link + external or future UI. |
| 31 | Email uniqueness; disabled accounts | **IMPLEMENTED** | Identity unique email; disable/enable users; login rejects disabled |
| 31 | Sensitive security ops audited | **IMPLEMENTED** | `LoginSucceeded` / `LoginFailed` / `UserRoleChanged` etc. |

### §32 Audit logging

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 32 | Auditable CMS/security events; immutable records with metadata/IP/UA | **IMPLEMENTED** | `AuditAction`, `AuditLogs`, API + `AuditLogView.vue`, domain immutability |

### §33–§42 Frontend

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 33 | Admin sections: Dashboard, Content, Content Types, Media, Users, Roles, Audit Log, **Settings**, **Profile** | **PARTIALLY IMPLEMENTED** | All except Settings/Profile implemented. **Gap:** Settings and Profile route to `SectionPlaceholderView.vue` (“later phase”) while SPEC lists them as required sections. |
| 34 | Protected routes by auth/permissions; access denied | **PARTIALLY IMPLEMENTED** | Guards + `AccessDeniedView`. **Gap:** Settings/Profile routes exist but are empty placeholders. |
| 35–36 | Dynamic content editor UX (fields, lifecycle, versions, confirmations) | **IMPLEMENTED** | Content entry views/components; E2E workflow |
| 37 | Dashboard from API | **IMPLEMENTED** | `DashboardController`, `DashboardView.vue` |
| 38 | Media UI | **IMPLEMENTED** | Library, upload, picker, metadata |
| 39 | Pinia for shared state (`authStore`, `notificationStore`, `uiStore`) | **IMPLEMENTED** | As amended in SPEC §39; domain data in views/composables |
| 40 | Dedicated API layer (not scattered HTTP) | **IMPLEMENTED** | `src/api/*` fetch client (**Axios not required**) |
| 41 | WCAG 2.1 AA reasonable practices | **PARTIALLY IMPLEMENTED** | eslint-a11y, skip links, modals, axe unit + E2E smoke. **Gap:** `docs/accessibility.md` known limits (rich text); axe not covering all CMS journeys; manual checklist open. |
| 42 | Responsive desktop/laptop/tablet | **IMPLEMENTED** | Admin shell responsive; mobile not primary |

### §43–§47 Database / EF / migrations / indexes / transactions

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 43 | Normalized system tables | **IMPLEMENTED** | EF model + migrations |
| 44 | EF Core with configurations | **IMPLEMENTED** | `Persistence/Configurations` |
| 45 | Committed migrations | **IMPLEMENTED** | PG + Azure SQL projects; CI validation |
| 46 | Indexes for common queries | **IMPLEMENTED** | Index migrations / configurations |
| 47 | Transactions for multi-step writes | **IMPLEMENTED** | Unit of work; publish/schedule txn fixes |

### §48–§49 Caching & rate limiting

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 48 | Cache public content; invalidate on publish changes | **IMPLEMENTED** | `MemoryPublicContentCache` + invalidation; integration tests. Distributed cache not required. |
| 49 | Rate limit login, password reset, public API, media upload | **IMPLEMENTED** | Rate limiting policies + tests + docs |

### §50–§53 Security, CORS, secrets, configuration

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 50 | Secure-by-default practices | **PARTIALLY IMPLEMENTED** | AuthZ, validation, parameterized EF, rate limits, secret externalization, headers. **Gap:** residual Azure public data-plane topology (acknowledged in Bicep); CSP not on SPA nginx. |
| 51 | Production CORS allowlist (no wildcard) | **IMPLEMENTED** | `Cors:AllowedOrigins` |
| 52 | Secrets not in git | **IMPLEMENTED** | Empty base secrets; Key Vault; env examples |
| 53 | Environment configuration | **IMPLEMENTED** | `appsettings*.json` + env vars |

### §54–§58 Logging, observability, health, correlation, OpenAPI

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 54 | Structured logging without secrets | **IMPLEMENTED** | Request logging; audit sanitizer |
| 55 | Application Insights | **IMPLEMENTED** | OTel Azure Monitor wiring; ops docs |
| 56 | `/health/live`, `/health/ready` | **IMPLEMENTED** | Health checks + tests |
| 57 | Correlation IDs | **IMPLEMENTED** | `CorrelationIdMiddleware` |
| 58 | OpenAPI in controlled environments | **IMPLEMENTED** | Swagger Dev/Testing |

### §59–§65 Testing

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 59–61 | Unit + integration strategy | **IMPLEMENTED** | `tests/ContentForge.UnitTests`, `IntegrationTests` |
| 62 | Architecture tests | **IMPLEMENTED** | `ContentForge.ArchitectureTests` |
| 63 | Frontend unit/component tests | **IMPLEMENTED** | Vitest under `frontend/.../tests/unit/` |
| 64 | E2E: auth, content, authorization, media | **IMPLEMENTED** | Playwright specs listed in `e2e/README.md` |
| 65 | Isolated test data | **IMPLEMENTED** | Fixtures/cleanup helpers |

### §66–§67 Code quality & dependencies

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 66 | Formatters/analyzers; TreatWarningsAsErrors posture | **IMPLEMENTED** | `dotnet format`, analyzers, ESLint/Prettier |
| 67 | Centralized package management | **IMPLEMENTED** | `Directory.Packages.props`, lockfiles |

### §68–§69 Docker

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 68 | Compose for local stack (Postgres, Azurite, API, web) | **IMPLEMENTED** | `docker-compose.yml` (+ test/e2e/prod variants) |
| 69 | Multi-stage, non-root containers | **IMPLEMENTED** | `infra/docker/api|web/Dockerfile` |

### §70–§71 CI/CD

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 70 | CI: restore, build, format, unit, integration, architecture, frontend, publish | **IMPLEMENTED** | `.github/workflows/ci.yml` |
| 71 | CD: build images, deploy, health verify; production protections | **PARTIALLY IMPLEMENTED** | `deploy.yml` + docs/scripts. **Gap:** live Azure deploy success **not proven** in current ops evidence (`PRODUCTION_READINESS_REPORT.md`). |

### §72–§80 Azure, backups, DR

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 72–73 | App Service topology, HTTPS, health, logging, MI | **IMPLEMENTED** | Bicep modules + ops docs |
| 74 | Azure SQL secure connectivity / backups / monitoring | **PARTIALLY IMPLEMENTED** | Azure AD auth, PITR/LTR in Bicep. **Gap:** public endpoint + `AllowAzureServices` residual; private endpoints **not** implemented. |
| 75–76 | Blob + managed identity | **IMPLEMENTED** | Storage module + MI RBAC |
| 77 | Staging / Production environments | **IMPLEMENTED** | Parameters + GH environments |
| 78 | Controlled DB deployment | **IMPLEMENTED** | Migrate-before-deploy; `database-migrations.md` |
| 79–80 | Backup/recovery documentation + DR awareness | **PARTIALLY IMPLEMENTED** | Runbooks + verify script. **Gap:** live DR drill evidence still required. |

### §81–§83 Performance, scalability, cancellation

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 81–83 | Async I/O, indexes, cancellation tokens | **IMPLEMENTED** | Async handlers, indexes, CT usage |
| 82 | Scalability awareness | **PARTIALLY IMPLEMENTED** | Single-instance App Service capacity; in-memory rate limit/cache. **Gap:** not horizontally scaled; documented limits. |

### §84–§88 Idempotency, background jobs, schedule, preview, schema evolution

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 84 | Idempotency where practical | **PARTIALLY IMPLEMENTED** | Strong for scheduled job claims. Lifecycle retries rely on domain transition rules (not no-op idempotent keys on every POST). |
| 85 | Background processing abstraction | **IMPLEMENTED** | Scheduler + hosted service |
| 86 | Scheduled publish/unpublish | **PARTIALLY IMPLEMENTED** | **Backend:** schedule API, jobs, processor, tests. **Gap:** **no frontend UI** to set/clear schedules. |
| 87 | Draft preview tokens | **IMPLEMENTED** | Preview token API + FE preview route; security tests |
| 88 | Schema evolution with destructive confirmations | **IMPLEMENTED** | Domain + FE destructive panels + tests |

### §89 Soft deletion

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 89 | Soft deletion recoverable | **PARTIALLY IMPLEMENTED** | `IsDeleted` on content/media; filtered from normal queries; `includeDeleted` list options; domain restore helpers exist. **Gap:** **no Application/API/UI command** to restore soft-deleted content/media found (`RestoreDeleted` not wired in Application). |

### §90–§94 Auditability, security testing, deps, FE security, a11y gates

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 90 | Explainability (who/what/when) | **IMPLEMENTED** | Audit + versions + timestamps |
| 91 | API security testing | **IMPLEMENTED** | Security/auth/media/rate-limit/preview/rich-text integration tests |
| 92 | Dependency security scanning | **IMPLEMENTED** | NuGet vulnerable list + npm audit in CI |
| 93 | Frontend security (no secrets; sanitization) | **IMPLEMENTED** | Custom rich-text sanitizer (**DOMPurify not mandated**); navigation safety |
| 94 | Accessibility/UX quality gates | **PARTIALLY IMPLEMENTED** | Automated smoke. Manual release checklist in `docs/accessibility.md` still open. |

### §95–§98 Documentation & DX

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 95 | Architecture, API, ops docs | **IMPLEMENTED** | `docs/` tree |
| 96 | ADRs | **IMPLEMENTED** | `docs/decisions/ADR-001` … `ADR-011` |
| 97–98 | Developer experience / local workflow | **IMPLEMENTED** | `README.md`, compose, documented commands |

### §99–§101 Seed / demo / SEO

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 99 | Dev seed: admin, editor, author, viewer, sample types/content/media/audit; never in Production | **PARTIALLY IMPLEMENTED** | Dev initializer seeds **administrator** + roles/permissions. Production does not seed. **Gap:** no deterministic editor/author/viewer users or sample content/media/audit pack in Development seed. |
| 100 | Demo content types Article/Page/Author/Category | **PARTIALLY IMPLEMENTED** | Achievable via dynamic types (E2E/tests create types). **Gap:** default demo environment does **not** seed these types automatically. |
| 101 | SEO metadata as content fields | **IMPLEMENTED** | Supported as ordinary fields (e.g. Page shape); no separate SEO engine required |

### §102–§104 API consumer experience / client generation / validation consistency

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 102 | Consumer docs/examples | **IMPLEMENTED** | `docs/api/README.md` |
| 103 | Client generation suitability (“may use”) | **PARTIALLY IMPLEMENTED** | OpenAPI available. Frontend uses hand-written clients (allowed by “may”). |
| 104 | Backend authoritative; FE validation UX | **IMPLEMENTED** | FluentValidation + FE validators |

### §105 Production readiness

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 105 | All checklist items true before “production-ready” | **NOT IMPLEMENTED** | SPEC boxes remain `[ ]`. Engineering artifacts exist for many lines, but **certification evidence is incomplete** (live Azure deploy/DR). [PRODUCTION_READINESS_REPORT.md](./PRODUCTION_READINESS_REPORT.md) explicitly refuses to claim readiness. **Do not treat code presence as §105 completion.** |

### §106–§115 Process / AI / git / release rules

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 106–115 | Definition of Done, AI rules, git/PR/release process | **NOT APPLICABLE** | Operating rules for humans/agents, not product runtime features. Tracked via `DEVELOPMENT_RULES.md` / `AGENTS.md`. |

### §116 Capability matrix

| Capability (SPEC §116) | Status | Notes |
|------------------------|--------|-------|
| User authentication … Frontend tests, CI/CD, Draft preview, Schema evolution, ADR docs, Backup/recovery documentation | **IMPLEMENTED** | See corresponding sections above |
| Scheduled publishing | **PARTIALLY IMPLEMENTED** | Backend yes; SPA UI no |
| Soft deletion | **PARTIALLY IMPLEMENTED** | Soft-delete yes; restore API/UI no |
| Azure App Service / Azure SQL / Blob | **IMPLEMENTED** | IaC + providers |
| RBAC / Permissions | **PARTIALLY IMPLEMENTED** | Works; code-matrix vs DB note under §8 |

### §117–§118 Portfolio / architectural principle

| ID | Requirement | Status | Evidence / gap |
|----|-------------|--------|----------------|
| 117 | Portfolio positioning narrative | **NOT APPLICABLE** | Positioning, not a build checklist item |
| 118 | Coherent modular monolith principle | **IMPLEMENTED** | Qualitative: Clean Architecture CMS rather than demo collage |

---

## Gap register (PARTIAL + NOT IMPLEMENTED only)

| SPEC | Status | Gap (no redefinition) |
|------|--------|------------------------|
| §8 | PARTIAL | Authz runtime source of truth is code matrix, not DB edits |
| §8 admin “system configuration” | PARTIAL | No system-config UI beyond roles list |
| §31 password reset | PARTIAL | API workflow exists; **SPA UI missing** |
| §33–§34 Settings/Profile | PARTIAL | Routes exist; **functionality not implemented** |
| §41 / §94 a11y | PARTIAL | Foundations yes; full AA evidence/manual gates open |
| §50 security | PARTIAL | Residual Azure public data plane; no CSP |
| §71 CD | PARTIAL | Pipeline exists; live success unproven |
| §74 / private endpoints | PARTIAL / NOT | Public SQL residual; private endpoints not built |
| §79–§80 DR | PARTIAL | Docs/scripts exist; live drill unproven |
| §82 scale | PARTIAL | Single-instance assumptions |
| §84 idempotency | PARTIAL | Strong for jobs; not universal on all POSTs |
| §86 schedule | PARTIAL | **No FE schedule UI** |
| §89 soft delete | PARTIAL | **No restore soft-deleted API/UI** |
| §99–§100 seed/demo | PARTIAL | Admin-only seed; no demo type pack |
| §103 clients | PARTIAL | OpenAPI yes; generated FE clients not used |
| §105 readiness | **NOT** | Checklist unchecked; readiness not certified |
| Password-reset SPA | **NOT** | No forgot/reset views |
| Settings/Profile features | **NOT** | Placeholders only |
| Private endpoints | **NOT** | Deferred hardening |

---

## How to use this document before release

1. Treat **NOT IMPLEMENTED** and **PARTIALLY IMPLEMENTED** rows as release triage input.  
2. Either implement the gap, or obtain an **explicit SPEC amendment / waiver** (do not silently redefine SPEC).  
3. Re-run this audit after closing gaps.  
4. §105 remains a separate evidence checklist — complete only with attached CI/Azure/DR proof.

---

## Related documents

- [SPECIFICATIONS.md](../../SPECIFICATIONS.md)  
- [PRODUCTION_READINESS_REPORT.md](./PRODUCTION_READINESS_REPORT.md)  
- [ARCHITECTURE.md](../ARCHITECTURE.md)  
- [IMPLEMENTATION_PLAN.md](../IMPLEMENTATION_PLAN.md)  

---

*End of specification compliance audit.*
