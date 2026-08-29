# ContentForge — Architecture

**Version:** 1.0  
**Status:** Planning  
**Source of Truth:** [SPECIFICATIONS.md](../SPECIFICATIONS.md)

This document describes the target architecture for ContentForge. It guides implementation and must remain consistent with the specification.

---

## 1. Architectural Principles

ContentForge follows **Clean Architecture** with strict inward dependency flow:

```text
Api → Application → Domain ← Infrastructure
```

Guiding principles (from SPEC §118):

- Clarity, correctness, testability, security, maintainability, observability
- Every technology choice must solve a real requirement
- Domain logic free of framework and infrastructure concerns
- Backend remains authoritative for validation, authorization, and business rules

---

## 2. Solution Structure

```text
/
├── src/
│   ├── ContentForge.Api/              # HTTP, auth middleware, controllers, OpenAPI
│   ├── ContentForge.Application/      # Use cases, DTOs, validators, interfaces
│   ├── ContentForge.Domain/           # Entities, value objects, domain rules
│   └── ContentForge.Infrastructure/   # EF Core, Identity, storage, external services
│
├── frontend/
│   └── contentforge-web/              # Vue 3 + TypeScript admin SPA
│
├── tests/
│   ├── ContentForge.UnitTests/
│   ├── ContentForge.IntegrationTests/
│   └── ContentForge.ArchitectureTests/
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── IMPLEMENTATION_PLAN.md
│   ├── DEVELOPMENT_RULES.md
│   ├── architecture/
│   ├── api/
│   ├── decisions/                     # ADRs
│   └── operations/
│
├── infra/
│   ├── docker/                        # Dockerfiles
│   └── azure/                         # Azure Bicep/ARM/Terraform (as chosen)
│
├── .github/workflows/
├── docker-compose.yml
├── docker-compose.test.yml
├── Directory.Build.props
└── Directory.Packages.props
```

### 2.1 Project Responsibilities

| Project | Responsibility |
|---------|----------------|
| **Domain** | Entities, enums, value objects, domain exceptions, domain services, repository interfaces |
| **Application** | Commands/queries (use cases), DTOs, FluentValidation validators, application service interfaces, mapping |
| **Infrastructure** | EF Core DbContext, entity configurations, Identity, JWT, blob storage, audit persistence, search implementation |
| **Api** | Controllers, middleware, filters, DI composition root, Swagger, health checks |
| **Frontend** | Admin UI only; consumes REST API |

---

## 3. Dependency Direction

```text
┌───────────────────────────────────────────────┐
│                  Vue Frontend                 │
│             Vue 3 + TypeScript               │
└───────────────────────┬───────────────────────┘
                        │ HTTPS / REST
                        ▼
┌───────────────────────────────────────────────┐
│                 ContentForge.Api              │
│        Controllers / Middleware / OpenAPI     │
└───────────────────────┬───────────────────────┘
                        │
                        ▼
┌───────────────────────────────────────────────┐
│            ContentForge.Application            │
│     Use Cases / DTOs / Validators / Ports     │
└───────────────────────┬───────────────────────┘
                        │
                        ▼
┌───────────────────────────────────────────────┐
│              ContentForge.Domain              │
│       Entities / Rules / Domain Events        │
└───────────────────────────────────────────────┘
                        ▲
                        │ implements interfaces
┌───────────────────────┴───────────────────────┐
│          ContentForge.Infrastructure           │
│   EF Core / Identity / Blob / Audit / Search  │
└───────────────────────────────────────────────┘
```

### 3.1 Forbidden Dependencies

The **Domain** layer must not reference:

- ASP.NET Core, EF Core, PostgreSQL, Azure SDKs, HTTP, Vue, or Infrastructure

The **Application** layer must not reference:

- Api, EF Core concrete types, Azure SDKs

**Infrastructure** implements interfaces defined in Domain/Application and is referenced only from Api (composition root).

Architecture tests enforce these rules (SPEC §62).

---

## 4. Backend Layers

### 4.1 Domain Layer

Contains pure business logic:

- **Entities:** `User`, `ContentType`, `ContentTypeField`, `ContentEntry`, `ContentVersion`, `Media`, `AuditLog`
- **Value objects:** `Slug`, `FieldDefinition`, `ContentStatus`, permission identifiers
- **Domain services:** lifecycle transition validator, slug generator, schema validator (rules only)
- **Exceptions:** `DomainException`, `ConflictException`, `ValidationException` (domain-level)
- **Repository interfaces:** `IContentEntryRepository`, `IContentTypeRepository`, etc.

No ORM attributes on domain entities where avoidable; EF configurations live in Infrastructure.

### 4.2 Application Layer

Orchestrates use cases:

- **Commands/queries:** `CreateContentEntry`, `PublishContent`, `RestoreVersion`, etc.
- **DTOs:** request/response models for API boundary
- **Validators:** FluentValidation for input DTOs; delegates schema validation to domain services
- **Interfaces (ports):** `IFileStorage`, `IAuditService`, `IContentSearchService`, `ICurrentUserService`, `IDateTimeProvider`
- **Mapping:** entity ↔ DTO (Manual or Mapster; decision in ADR if needed)

Application services coordinate transactions via unit-of-work abstraction; they do not perform HTTP or SQL directly.

### 4.3 Infrastructure Layer

Implements external concerns:

- **Persistence:** EF Core `AppDbContext`, entity configurations, migrations, repositories
- **Identity:** ASP.NET Core Identity with custom user store if needed
- **Authentication:** JWT token generation/validation services
- **Storage:** `LocalFileStorage`, `AzuriteBlobStorage`, `AzureBlobStorage` implementing `IFileStorage`
- **Audit:** `EfAuditService` writing immutable audit records
- **Search:** `EfContentSearchService` (initial); swappable per `IContentSearchService`
- **Background jobs:** `IBackgroundJobScheduler` abstraction (in-process or Azure WebJobs later)

Database-specific SQL (full-text, JSON operators) isolated here.

### 4.4 API Layer

Thin HTTP adapter:

- **Controllers:** Admin (`/api/v1/...`) and Public (`/api/v1/public/...`) separated by namespace/route conventions
- **Middleware:** exception handling, correlation ID, request logging, rate limiting
- **Authorization:** policy-based; maps permissions to ASP.NET authorization policies
- **Filters:** model validation, concurrency conflict mapping to 409
- **OpenAPI:** Swashbuckle with JWT security scheme
- **Health checks:** liveness and readiness
- **DI registration:** `Program.cs` / extension methods wire Infrastructure → Application → Api

---

## 5. Frontend Structure

```text
frontend/contentforge-web/
├── src/
│   ├── api/                    # Centralized API clients
│   │   ├── client.ts           # Axios/fetch wrapper, auth headers, error handling
│   │   ├── auth.ts
│   │   ├── content.ts
│   │   ├── contentTypes.ts
│   │   ├── media.ts
│   │   ├── users.ts
│   │   └── audit.ts
│   ├── stores/                 # Pinia
│   │   ├── authStore.ts
│   │   ├── contentStore.ts
│   │   ├── contentTypeStore.ts
│   │   ├── mediaStore.ts
│   │   ├── userStore.ts
│   │   └── auditStore.ts
│   ├── router/
│   │   ├── index.ts
│   │   └── guards.ts           # Auth + permission guards
│   ├── views/                  # Route-level pages
│   ├── components/
│   │   ├── layout/
│   │   ├── content/            # Dynamic editor, field renderers
│   │   ├── media/
│   │   └── common/
│   ├── composables/            # Reusable logic
│   ├── types/                  # TypeScript interfaces (may include OpenAPI-generated)
│   └── utils/                  # Sanitization, formatting
├── tests/
│   ├── unit/
│   └── component/
└── e2e/                        # Playwright/Cypress (Phase 16)
```

### 5.1 Frontend Architecture Rules

- **API access** only through `src/api/` — never scattered in components
- **Global state** in Pinia stores; transient UI state stays local
- **Route protection** via guards checking auth token and permissions
- **Validation** mirrors backend rules for UX; backend remains authoritative
- **Rich text** rendered through sanitization (DOMPurify or equivalent)
- **Strict TypeScript** (`strict: true` in tsconfig)

---

## 6. Persistence Strategy

### 6.1 Database Providers

| Environment | Database |
|-------------|----------|
| Development | PostgreSQL (Docker Compose) |
| Test / CI | PostgreSQL (Docker Compose / test container) |
| Staging / Production | Azure SQL Database |

EF Core abstracts provider differences. Database-specific features (e.g., JSON querying, full-text) isolated in Infrastructure with provider-conditional implementations where necessary.

### 6.2 Schema Design

Normalized system tables (SPEC §43):

```text
Users, Roles, Permissions, UserRoles, RolePermissions
ContentTypes, ContentTypeFields
ContentEntries, ContentVersions
Media
AuditLogs
+ relation junction tables as needed
```

### 6.3 Content Data Storage

Clear separation:

| Concern | Storage |
|---------|---------|
| Content schema | `ContentTypes`, `ContentTypeFields` (relational) |
| Content data | `ContentEntries.Data` (JSON column) + typed indexes where needed |
| Published snapshot | Separate column/table or version reference (per ADR-007) |
| System metadata | Relational columns on `ContentEntries` |

Dynamic field data uses JSON; validation enforced in Application/Domain against schema, not by DB constraints alone.

### 6.4 EF Core Conventions

- Explicit `IEntityTypeConfiguration<T>` per entity
- Migrations committed to source control; reviewed and tested
- `RowVersion` / concurrency token on `ContentEntry` and other concurrently edited entities
- Indexes on: `ContentTypeId`, `Status`, `Slug`, `CreatedAt`, `UpdatedAt`, `ContentEntryId` (versions), `AuditLog.Timestamp`, `AuditLog.UserId`
- Async APIs only; `CancellationToken` propagated
- No lazy loading by default; explicit includes/projections to avoid N+1

### 6.5 Transaction Boundaries

Explicit transactions for atomic operations:

- Publish content
- Restore version
- Delete content (soft delete + relation cleanup)
- Content type schema change

Transactions must not span external network calls (blob upload happens before/after transaction as designed).

---

## 7. Authentication Architecture

```text
┌──────────┐    POST /auth/login     ┌─────────────┐
│  Client  │ ──────────────────────► │   Api       │
└──────────┘                         └──────┬──────┘
     ▲                                      │
     │         JWT access token              ▼
     │                              ┌─────────────────┐
     └──────────────────────────────│ Identity + JWT  │
                                    │   Infrastructure │
                                    └─────────────────┘
```

### 7.1 Components

- **ASP.NET Core Identity:** user store, password hashing (PBKDF2/bcrypt via Identity defaults), lockout, email uniqueness
- **JWT bearer authentication:** access tokens with expiration, claims for user ID and permissions
- **Refresh tokens (optional):** if implemented, stored securely server-side or as rotating tokens; document in ADR-004
- **Token invalidation:** short-lived access tokens; refresh rotation or token blocklist for logout

### 7.2 Security Controls

- Password policies (complexity, length)
- Account lockout / login throttling
- Rate limiting on login and password reset
- Disabled account rejection at authentication and authorization
- Passwords never logged
- Secure password reset workflow with audit events

### 7.3 Frontend Auth Flow

1. User submits credentials to `/api/v1/auth/login`
2. Store receives JWT; attached to subsequent requests via API client interceptor
3. Token expiration triggers re-login or silent refresh (if implemented)
4. Logout clears client token and calls server invalidation endpoint if available

---

## 8. Authorization Architecture

Authorization is **permission-based** internally; roles are the administrative grouping.

```text
Role (Administrator, Editor, Author, Viewer)
    │
    └──► Permissions (content.read, content.publish, media.upload, ...)
              │
              └──► ASP.NET Authorization Policies
                        │
                        └──► Controller/action [Authorize(Policy = "...")]
```

### 8.1 Permission Enforcement

- Permissions stored in database: `Permissions`, `RolePermissions`
- JWT includes permission claims (or role claims resolved server-side per request — decision in ADR-004)
- Centralized `PermissionAuthorizationHandler` evaluates requirements
- Every protected mutation verifies permissions server-side
- Frontend permission checks are UX only (hide/disable controls)

### 8.2 Permission Catalog

Initial permissions per SPEC §8:

```text
content.read | content.create | content.update | content.delete
content.publish | content.archive | content.restore | content.review
content.version.read | content.version.restore
contentType.read | contentType.create | contentType.update | contentType.delete
media.read | media.upload | media.update | media.delete
user.read | user.create | user.update | user.disable
audit.read
```

### 8.3 Role → Permission Mapping

| Role | Capabilities |
|------|-------------|
| Administrator | All permissions |
| Editor | Content CRUD, review, publish, media, no user/role management |
| Author | Create, edit own drafts, submit for review, manage own content |
| Viewer | Read-only admin and published content |

"Own content" enforced in Application layer by comparing `CreatedBy` / ownership rules, not only by permission name.

---

## 9. Content Modeling Strategy

### 9.1 Dynamic Content Types

Content types are **data-defined**, not hardcoded C# classes (SPEC §100):

- `ContentType` entity holds metadata (Name, Slug, DisplayName, Version, IsActive)
- `ContentTypeField` defines field name, type, and JSON configuration
- Demo types (Article, Page, Author, Category) seeded as content type records

### 9.2 Field Type System

Each field type maps to:

- **Validation rules** in Domain (required, length, range, pattern, options)
- **Storage** as JSON key in `ContentEntry.Data`
- **Relation fields** store referenced entry IDs; validated for existence and type compatibility
- **Media fields** store media IDs; validated against media library

### 9.3 Schema vs Data vs Metadata

| Layer | Examples |
|-------|----------|
| Schema | Content type name, field definitions, validation config |
| Data | title, body, tags — stored in JSON `Data` |
| Metadata | Status, Slug, CreatedAt, PublishedAt, CurrentVersion, RowVersion |

### 9.4 Schema Evolution

Changes to content types follow explicit rules:

- **Add field:** safe; optional fields or defaults
- **Remove field:** data retained in JSON but ignored; confirmation required
- **Rename field:** migration script or alias mapping; confirmation required
- **Change validation:** re-validate existing entries on next edit/publish
- **Change relation target:** blocked if incompatible entries exist

Destructive changes require admin confirmation workflow (API flag + UI confirmation).

---

## 10. Versioning Strategy

```text
ContentEntry (current head)
    │
    ├── ContentVersion 1  (immutable snapshot)
    ├── ContentVersion 2
    └── ContentVersion N  (latest)
```

### 10.1 Version Creation

A new `ContentVersion` is created on every meaningful mutation:

- Content data changes
- Status transitions (especially publish)
- Slug changes on published content
- Version restore

Each version contains: `VersionNumber` (monotonic per entry), `Snapshot` (JSON), `CreatedAt`, `CreatedBy`, `ChangeSummary`.

### 10.2 Draft vs Published

- **Draft data** lives on the entry's working copy
- **Published representation** is a snapshot promoted on publish (either copied to dedicated published storage or referenced via published version pointer — ADR-007)
- Editing draft after publish does not alter public API until re-published

### 10.3 Restore

Restoring version N:

1. Load immutable snapshot from version N
2. Apply as new draft/working copy
3. Create version N+1 documenting the restore
4. Historical versions remain unchanged

### 10.4 Compare

Version compare returns structured diff (field-level added/changed/removed) computed in Application layer from JSON snapshots.

---

## 11. Media Storage Abstraction

```text
         IFileStorage (Application/Domain port)
                │
    ┌───────────┼───────────────┐
    ▼           ▼               ▼
LocalFile   AzuriteBlob    AzureBlobStorage
Storage     Storage        (Production)
(Dev)       (Dev/Test)
```

### 11.1 Interface Responsibilities

```csharp
// Conceptual — exact signature defined at implementation
interface IFileStorage
{
    Task<StorageResult> UploadAsync(Stream content, StorageUploadRequest request, CancellationToken ct);
    Task<Stream> DownloadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct);
}
```

### 11.2 Storage Key Generation

- System-generated GUID-based keys with controlled prefix (e.g., `media/{yyyy}/{guid}.ext`)
- User filenames stored as metadata only (`OriginalFileName`)
- Path traversal prevented; uploads treated as untrusted

### 11.3 Media Entity

Relational DB stores metadata only (`StorageKey`, `Url`, dimensions, alt text). Binaries never in SQL.

### 11.4 Upload Validation

MIME type, extension allowlist, max size, filename sanitization — enforced in Application before storage call.

---

## 12. Public vs Administrative API

### 12.1 Route Separation

| Surface | Base Path | Auth | Content |
|---------|-----------|------|---------|
| **Public** | `/api/v1/public/` | None (rate limited) | Published only |
| **Admin** | `/api/v1/auth/`, `/users/`, `/roles/`, `/content-types/`, `/content/`, `/media/`, `/audit/` | JWT required | Full CMS |

Preview endpoints (authenticated, scoped tokens) live under admin namespace, not public.

### 12.2 DTO Separation

- **Admin DTOs:** include status, audit fields, draft data, internal IDs, concurrency tokens
- **Public DTOs:** stable, minimal, no internal metadata, no user info unless modeled as public content (e.g., Author name field)

Public DTOs versioned independently if needed within `/api/v1/`.

### 12.3 Query Behavior

| Feature | Admin API | Public API |
|---------|-----------|------------|
| Drafts | Visible per permissions | Never exposed |
| Filtering | Full whitelist filters | Limited (status always published) |
| Search | Full admin search | Optional basic filtering |
| Pagination | Yes | Yes |
| Sorting | Controlled whitelist | Controlled whitelist |

---

## 13. Content Lifecycle

```text
DRAFT ──► IN_REVIEW ──► PUBLISHED ──► ARCHIVED
  ▲          │              │
  │          ▼              ▼
  └──────── DRAFT      UNPUBLISHED ──► DRAFT
```

Valid transitions enforced in Domain (`ContentLifecycleService`):

| From | To |
|------|-----|
| DRAFT | IN_REVIEW |
| IN_REVIEW | DRAFT, PUBLISHED |
| PUBLISHED | DRAFT, ARCHIVED |
| ARCHIVED | DRAFT |
| PUBLISHED | UNPUBLISHED → DRAFT |

Invalid transitions → domain error → HTTP 422/409 as appropriate.

**Publish** executes: schema validation → version creation → published snapshot update → publication metadata → cache invalidation → audit event.

---

## 14. Search Architecture

```text
Admin API ──► IContentSearchService (Application port)
                      │
                      ▼
              EfContentSearchService (Infrastructure)
                      │
              (future: Azure Cognitive Search, Elasticsearch, etc.)
```

Initial implementation uses EF Core with PostgreSQL `ILIKE`/JSON operators. Domain and Application depend only on the interface. Filters and sort fields are whitelisted; unsupported parameters return 400.

---

## 15. Audit Architecture

- Append-only `AuditLogs` table
- `IAuditService` called from Application use cases after successful operations
- Records: Timestamp, UserId, Action, EntityType, EntityId, Metadata (JSON), IpAddress, UserAgent
- No update/delete API for audit records
- Admin read access requires `audit.read` permission

---

## 16. Error Handling Architecture

All API errors use **RFC 7807 Problem Details**:

```json
{
  "type": "https://contentforge/errors/validation",
  "title": "Validation failed",
  "status": 422,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/content/articles/123",
  "errors": { "title": ["Title is required."] },
  "traceId": "..."
}
```

- Global exception middleware maps domain exceptions to status codes
- Validation errors → 422 with field-level `errors`
- Concurrency conflict → 409 with current version info for client resolution
- Production: no stack traces or internal details in responses

---

## 17. Testing Architecture

```text
tests/
├── ContentForge.UnitTests/           # Domain, Application (no DB)
├── ContentForge.IntegrationTests/    # WebApplicationFactory + PostgreSQL
├── ContentForge.ArchitectureTests/   # NetArchTest / custom rules
frontend/contentforge-web/
├── tests/unit/                       # Vitest: stores, utils
├── tests/component/                  # Vue Test Utils
└── e2e/                              # Playwright: critical journeys
```

| Layer | Scope | Tools |
|-------|-------|-------|
| Unit | Domain rules, validators, lifecycle, permissions, slug, versioning | xUnit, FluentAssertions |
| Integration | HTTP endpoints, auth, EF, migrations, transactions, errors | xUnit, WebApplicationFactory, Testcontainers/Docker |
| Architecture | Dependency direction, layer isolation | NetArchTest |
| Frontend unit | Stores, API client, composables | Vitest |
| Frontend component | Dynamic forms, guards | Vue Test Utils |
| E2E | Login, content workflow, authorization, media | Playwright |

Test data via factories/builders; isolated per test; no production data; order-independent.

---

## 18. Docker Architecture

### 18.1 Local Development (`docker-compose.yml`)

```text
┌─────────────────────────────────────────────────┐
│  docker compose                                  │
│  ┌──────────────┐  ┌──────────────┐             │
│  │  PostgreSQL  │  │   Azurite    │             │
│  │   :5432      │  │   :10000     │             │
│  └──────────────┘  └──────────────┘             │
└─────────────────────────────────────────────────┘
         ▲                    ▲
         │                    │
   dotnet run            dotnet run
   (host machine)        (host machine)
   npm run dev
```

Backend and frontend run on host during development for fast iteration; infrastructure in Docker.

Optional: full stack compose with API and frontend containers for CI-like local testing.

### 18.2 Test Compose (`docker-compose.test.yml`)

Ephemeral PostgreSQL (and Azurite if needed) for integration tests and CI.

### 18.3 Production Dockerfile

Multi-stage build:

1. **Build stage:** SDK image, restore, publish
2. **Runtime stage:** ASP.NET runtime, non-root user, minimal footprint

Frontend: static files built in CI; served via App Service static hosting, CDN, or embedded static files middleware (ADR-010).

### 18.4 Container Requirements (SPEC §69)

- Non-root execution
- Multi-stage builds
- Configuration via environment variables
- Only required ports exposed

---

## 19. Azure Deployment Architecture

```text
                    Internet
                       │
                       ▼
               Azure App Service
              (HTTPS termination)
                       │
         ┌─────────────┴─────────────┐
         │                           │
         ▼                           ▼
   ASP.NET Core API            Vue Static Assets
   (App Service)               (same or separate App Service / CDN)
         │
    ┌────┼────────────┐
    ▼    ▼            ▼
Azure SQL  Azure Blob  Application
Database   Storage     Insights
```

### 19.1 Components

| Service | Purpose |
|---------|---------|
| **Azure App Service** | Host ASP.NET Core API; optionally frontend static site |
| **Azure SQL Database** | Production relational persistence |
| **Azure Blob Storage** | Media binaries |
| **Application Insights** | Telemetry, logs, dependencies, exceptions |
| **Azure Key Vault** (recommended) | Secrets, connection strings |
| **Managed Identity** | App Service → SQL, Blob, Key Vault (no long-lived credentials) |

### 19.2 Environments

| Environment | Purpose |
|-------------|---------|
| Development | Local Docker + host-run apps |
| Test | CI ephemeral containers |
| Staging | Production-like Azure deployment for validation |
| Production | Live deployment with approval gates |

### 19.3 Deployment Flow

```text
GitHub Actions CI → Build artifacts → Container image → Deploy to Staging
    → Health check (/health/ready) → Manual/auto promote to Production
    → Migration execution (controlled, documented) → Verification
```

- Deployment slots for zero-downtime swaps where appropriate
- Database migrations applied via documented pipeline step — not implicit auto-migrate on startup in Production
- Backups: Azure SQL automated backups + point-in-time recovery; Blob redundancy (GRS/LRS per policy)

### 19.4 Configuration

Environment variables and Key Vault references:

- Connection strings (SQL, Blob)
- JWT signing keys
- CORS allowed origins
- Application Insights connection string
- Rate limit thresholds

Secrets never in source, Docker images, or workflow files.

### 19.5 Observability in Azure

- Application Insights SDK in API
- Structured logs with correlation IDs forwarded to Insights
- Health checks configured as App Service health probes
- Alerts on failed health checks, exception rate spikes (operations docs)

---

## 20. Cross-Cutting Concerns

### 20.1 Caching

Optional ASP.NET output/data cache for public content. Invalidated on publish/unpublish/archive. Not required for initial release but architecture supports it.

### 20.2 Rate Limiting

Applied to: login, password reset, public API, media upload. Configurable per environment via `appsettings`.

### 20.3 Background Processing

`IBackgroundJobScheduler` abstraction for scheduled publishing (`publishAt`, `unpublishAt`). Initial implementation may use `IHostedService` with idempotent execution and persisted job state. Not a full distributed scheduler unless justified.

### 20.4 Correlation IDs

Middleware accepts `X-Correlation-ID` from trusted sources or generates GUID. Propagated to logs, Application Insights, and error responses (`traceId`).

### 20.5 API Versioning

All external APIs under `/api/v1/`. Breaking changes require `/api/v2/`. Versioned via URL path (explicit, per SPEC).

### 20.6 Soft Deletion

Content entries (and potentially media) use soft delete flags. Excluded from normal queries; recoverable; audit trail preserved.

---

## 21. Security Architecture Summary

| Control | Implementation |
|---------|---------------|
| Transport | HTTPS in production |
| Authentication | Identity + JWT |
| Authorization | Permission policies, server-side |
| Input validation | FluentValidation + domain schema validation |
| SQL injection | EF Core parameterized queries |
| XSS | Output encoding; sanitized rich text in frontend |
| File upload | Type/size validation; safe storage paths |
| Secrets | Key Vault / App Service settings |
| CORS | Explicit allowlist |
| Rate limiting | Middleware |
| Audit | Immutable audit log |

---

## 22. Related Documents

- [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) — phased delivery
- [DEVELOPMENT_RULES.md](./DEVELOPMENT_RULES.md) — coding and agent rules
- [SPECIFICATIONS.md](../SPECIFICATIONS.md) — authoritative requirements
- `docs/decisions/` — ADRs for key decisions
