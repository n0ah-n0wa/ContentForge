# ContentForge — Architecture

**Version:** 1.1  
**Status:** As built  
**Source of Truth (requirements):** [SPECIFICATIONS.md](../SPECIFICATIONS.md)  
**Decisions:** [docs/decisions/](./decisions/)

This document describes the **implemented** architecture. Supplementary reviews live under [docs/architecture/](./architecture/).

---

## 1. Architectural principles

ContentForge follows **Clean Architecture** with inward dependency flow:

```text
Api → Application → Domain ← Infrastructure
                  ← Infrastructure.SqlServer (Azure SQL migrations)
```

- Domain logic is free of ASP.NET, EF, and Azure SDK types
- Application orchestrates use cases via ports (interfaces)
- Infrastructure implements persistence, Identity, storage, search, and background jobs
- Api is the composition root (HTTP, auth middleware, DI, OpenAPI, health)
- **Backend is authoritative** for validation, authorization, and business rules
- Architecture tests (`ContentForge.ArchitectureTests`) enforce forbidden references

See [ADR-001](./decisions/ADR-001-clean-architecture.md).

---

## 2. Solution structure

```text
/
├── src/
│   ├── ContentForge.Domain/
│   ├── ContentForge.Application/
│   ├── ContentForge.Infrastructure/           # PostgreSQL migrations + shared infra
│   ├── ContentForge.Infrastructure.SqlServer/ # Azure SQL migrations
│   └── ContentForge.Api/
├── frontend/contentforge-web/                 # Vue 3 + TypeScript admin SPA
├── tests/
│   ├── ContentForge.UnitTests/
│   ├── ContentForge.IntegrationTests/
│   └── ContentForge.ArchitectureTests/
├── docs/                                      # architecture, api, decisions, operations
├── infra/
│   ├── docker/                                # API/Web Dockerfiles, env examples
│   └── azure/                                 # Bicep + deploy scripts
├── .github/workflows/                         # ci.yml, deploy.yml
├── docker-compose.yml
├── docker-compose.prod.yml
├── docker-compose.test.yml
└── docker-compose.e2e.yml
```

| Project | Responsibility |
|---------|----------------|
| **Domain** | Entities, value objects, permissions, lifecycle/schema rules, domain exceptions |
| **Application** | Commands/queries, DTOs, FluentValidation, ports, mapping, sanitization |
| **Infrastructure** | EF Core (PostgreSQL), Identity, JWT, blob/local storage, audit, EF search, scheduled publishing host |
| **Infrastructure.SqlServer** | Azure SQL–targeted EF migrations for cloud |
| **Api** | Controllers, middleware, rate limiting, Swagger, health, DI |
| **Frontend** | Admin UI only; typed `fetch` API client |

---

## 3. Runtime topology

### 3.1 Local development (Docker Compose)

```text
Browser ──► web (host :8080 → container :8080, nginx SPA + /api + /media-files proxy)
                │
                ▼
            api (host :5080 → container :8080) ──► postgres (host :5432)
                     └──► local disk and/or azurite (host :10000) for media
```

### 3.2 Azure (staging / production)

```text
Internet
   │
   ▼
app-cf-web-{env}  (public App Service, HTTPS)
   │  nginx proxies /api and /media-files
   ▼
app-cf-api-{env}  (restricted public access in staging/prod)
   ├── Azure SQL (Azure AD + managed identity)
   ├── Blob Storage media container (MI RBAC)
   └── Key Vault (JWT signing key reference)
        │
        └── Application Insights ← Log Analytics
```

See [ADR-010](./decisions/ADR-010-azure-hosting-topology.md) and [operations/azure-infrastructure.md](./operations/azure-infrastructure.md).

---

## 4. Backend layers

### 4.1 Domain

Notable areas:

- Content types & fields (`FieldType`: Text, LongText, RichText, Integer, Decimal, Boolean, Date, DateTime, Media, MediaMultiple, Relation, RelationMultiple, Select, MultiSelect, Json)
- Content entries, versions, lifecycle transitions, optimistic concurrency
- Media assets and upload rules (extension/content-type/size validation)
- Roles and permission catalog (`Permissions` / `DefaultRoleDefinitions`)
- Audit event kinds and metadata sanitization rules

### 4.2 Application

- Handlers for auth, users, content types, content lifecycle, versions, media, audit, dashboard
- `IFileStorage`, `IContentSearchService`, `IAuditService`, `ICurrentUserService`, etc.
- Rich-text sanitization (`HtmlSanitizer` wrapper) before persistence
- Scheduled publishing command handlers + job processor interface usage

### 4.3 Infrastructure

- `AppDbContext`, entity configurations, repositories
- ASP.NET Identity + JWT issuance/validation
- `LocalFileStorage` / Azure Blob implementations of `IFileStorage`
- `EfContentSearchService` — portable keyword contains + filters/sort/pagination ([ADR-008](./decisions/ADR-008-search-abstraction.md))
- `ScheduledPublishingBackgroundService` + job claim/processing ([ADR-011](./decisions/ADR-011-scheduled-publishing.md))
- Development DB initializer (migrations + seed) — **Development only**

### 4.4 API

| Route prefix | Controllers |
|--------------|-------------|
| `/api/v1/auth` | Login, logout, refresh, password reset, me |
| `/api/v1/users` | User CRUD, disable/enable |
| `/api/v1/roles` | Role list |
| `/api/v1/content-types` | Types and fields |
| `/api/v1/content` | Entries, lifecycle, schedule, versions, search, preview |
| `/api/v1/public` | Published content read API |
| `/api/v1/media` | Media metadata CRUD / upload |
| `/media-files` | Binary file serving |
| `/api/v1/audit` | Audit query |
| `/api/v1/dashboard` | Dashboard stats |
| `/health/live`, `/health/ready` | Health |

OpenAPI (Swashbuckle) is enabled in **Development** and **Testing** environments.

---

## 5. Frontend structure (as built)

```text
frontend/contentforge-web/src/
├── api/              # fetch client + domain modules (auth, content, media, …)
├── stores/           # Pinia: authStore, notificationStore, uiStore only
├── router/           # routes + auth/permission guards
├── views/            # route-level pages
├── components/       # layout, forms, media, content fields, dialogs
├── composables/      # forms, permissions, modal, errors
├── types/
└── utils/            # sanitizer helpers, navigation safety, …
```

Rules:

- HTTP only via `src/api/` (`client.ts` — **native fetch**, not Axios)
- Domain state lives in views/composables; Pinia is limited to session, toasts, and loading UI
- Route `meta.permissions` enforced by guards; mutation routes require update permissions
- Strict TypeScript; unit tests (Vitest) + E2E (Playwright)

Accessibility notes: [frontend/contentforge-web/docs/accessibility.md](../frontend/contentforge-web/docs/accessibility.md).  
Frontend review: [architecture/final-frontend-review.md](./architecture/final-frontend-review.md).

---

## 6. Persistence

| Environment | Database | Migrations project |
|-------------|----------|-------------------|
| Development / CI / local Docker | PostgreSQL | `ContentForge.Infrastructure` |
| Staging / Production | Azure SQL | `ContentForge.Infrastructure.SqlServer` |

See [ADR-002](./decisions/ADR-002-database-strategy.md).

### Content data model

| Concern | Storage |
|---------|---------|
| Schema | Relational `ContentTypes` / `ContentTypeFields` |
| Draft field data | JSON column `DraftDataJson` on entries |
| Published representation | `PublishedSnapshotJson` (+ metadata such as `PublishedAt`) — [ADR-007](./decisions/ADR-007-publishing-architecture.md) |
| Versions | `ContentVersions` with immutable snapshots |
| Media metadata | Relational `Media`; binaries in file store / blob |
| Audit | Append-only audit table |

Dynamic JSON is validated in Domain/Application against the content-type schema ([ADR-003](./decisions/ADR-003-dynamic-content-schema.md)).

---

## 7. Authentication & authorization

```text
Client ──POST /auth/login──► Identity password check
                │
                ├── Access JWT (permissions/role claims)
                └── Refresh token (persisted; rotation on refresh)

Request ──Bearer JWT──► JwtBearer middleware
                │
                └── PermissionAuthorizationHandler (policy per permission)
```

Details: [ADR-004](./decisions/ADR-004-authentication-strategy.md), [architecture/security-auth-review.md](./architecture/security-auth-review.md).

Roles: Administrator (all permissions), Editor, Author, Viewer — see Domain `DefaultRoleDefinitions`.

---

## 8. Media storage

`IFileStorage` abstraction ([ADR-005](./decisions/ADR-005-media-storage.md)):

| Mode | Typical use |
|------|-------------|
| `Media:Provider=Local` | Local Docker / native disk under configured root |
| `Media:Provider=Azure` | Azurite locally or Azure Blob with connection string / managed identity |

Uploads are content-inspected; SVG and other risky types follow hardened rules (see security reviews).

---

## 9. Content versioning & publishing

- Edits create or advance versions with change summaries ([ADR-006](./decisions/ADR-006-content-versioning.md))
- Publish copies validated draft into published snapshot; public API reads published only
- Soft delete for entries/media where specified; restore flows where implemented
- Optimistic concurrency via `concurrencyToken` → HTTP 409

---

## 10. Cross-cutting concerns

| Concern | Implementation |
|---------|----------------|
| Errors | Problem Details–style responses; frontend toast mapping |
| Rate limiting | ASP.NET rate limiter policies (auth, public, media, preview, …) |
| Caching | Configurable public content cache options |
| Observability | Structured logging; Application Insights / OpenTelemetry when enabled |
| Health | `/health/live`, `/health/ready` (DB readiness) |
| CI quality | Format, NuGet/npm audit, unit/arch/integration, frontend gates, E2E, Bicep/Docker smoke |

---

## 11. Deployment & migrations

- **Local Development:** API may apply PostgreSQL migrations on startup
- **Staging/Production:** Explicit pipeline step via `infra/azure/scripts/run-azure-sql-migrations.sh` — API does **not** migrate on startup ([ADR-009](./decisions/ADR-009-cicd-and-migrations.md))

Ops guides: [operations/](./operations/).

---

## 12. Related documents

- [API usage & examples](./api/README.md)
- [Implementation plan](./IMPLEMENTATION_PLAN.md)
- [Final backend review](./architecture/final-backend-review.md)
- [Final frontend review](./architecture/final-frontend-review.md)
- [Azure security model](./operations/azure-security.md)
