# ContentForge — Development Rules

**Version:** 1.0  
**Status:** Planning  
**Source of Truth:** [SPECIFICATIONS.md](../SPECIFICATIONS.md)

These rules govern all human and AI-assisted development on ContentForge. They complement [ARCHITECTURE.md](./ARCHITECTURE.md) and [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md).

---

## 1. Source of Truth

1. **`SPECIFICATIONS.md` is authoritative.** Do not silently redefine requirements.
2. When implementation conflicts with the specification:
   - Identify the conflict explicitly
   - Explain the impact
   - Propose a resolution
   - Wait for explicit approval before architectural or breaking changes
3. Architecture documents and ADRs must not contradict the specification.

---

## 2. Coding Conventions

### 2.1 General

- **Nullable reference types** enabled project-wide; warnings treated seriously
- **No dead code**, no commented-out production code, no unexplained magic values
- **Consistent naming:** PascalCase for public members (C#), camelCase for locals/parameters, `_camelCase` for private fields
- **Async all the way:** suffix async methods with `Async`; accept `CancellationToken` on I/O-bound methods
- **XML documentation** on public APIs where behavior is non-obvious
- **Formatting enforced** via `.editorconfig` and `dotnet format`

### 2.2 C# / Backend

- Follow standard .NET naming and layout conventions
- One public type per file (exceptions for small related enums/records)
- Prefer `record` for immutable DTOs and value objects where appropriate
- Use `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>` on public collection returns
- Avoid `dynamic`; avoid `null` propagation without explicit intent
- Domain entities: behavior-rich where logic belongs in domain; anemic only for pure data with validation elsewhere
- Controllers remain thin — no business logic in controllers
- Use `Directory.Packages.props` for centralized package version management

### 2.3 TypeScript / Frontend

- **Strict TypeScript** (`strict: true`, `noImplicitAny`, `strictNullChecks`)
- Vue 3 Composition API with `<script setup lang="ts">`
- Component names: PascalCase multi-word (e.g., `ContentEditor.vue`)
- Composables: `use` prefix (e.g., `useContentEditor`)
- Pinia stores: `defineStore` with typed state/actions/getters
- No `any` without inline justification comment
- Prefer `const`; never use `var`

### 2.4 File Organization

- Match repository structure defined in SPEC §6 and ARCHITECTURE.md
- New projects or top-level folders require documented architectural reason
- Place ADRs in `docs/decisions/` when making significant decisions

---

## 3. Dependency Rules

### 3.1 Layer Dependencies

| Layer | May Reference | Must NOT Reference |
|-------|---------------|-------------------|
| Domain | (nothing external) | Api, Application, Infrastructure, EF, ASP.NET, Azure SDKs |
| Application | Domain | Api, Infrastructure concretes, EF, ASP.NET |
| Infrastructure | Domain, Application | Api |
| Api | Application, Infrastructure, Domain | — (composition root) |

### 3.2 Adding Dependencies

- Every NuGet/npm package must be **justified** — actively maintained, minimal scope
- Add versions to `Directory.Packages.props` (backend) or `package.json` (frontend)
- Remove unused dependencies promptly
- Do not introduce alternatives to mandated stack (e.g., React instead of Vue) without spec amendment

### 3.3 Interface Segregation

- Infrastructure implements interfaces defined in Domain or Application
- Application depends on abstractions (`IFileStorage`, `IAuditService`, `IContentSearchService`), never concrete Infrastructure types
- Frontend depends on API contracts, not backend internals

### 3.4 Architecture Tests

Every PR touching project references must pass architecture tests verifying forbidden dependency directions (SPEC §62).

---

## 4. Testing Expectations

### 4.1 Philosophy

Testing is first-class. Behavior changes require tests. Tests must be **deterministic**, **isolated**, and **order-independent**.

### 4.2 Required Test Types

| Type | Location | When Required |
|------|----------|---------------|
| Unit | `ContentForge.UnitTests` | Domain rules, validators, use cases, permissions |
| Integration | `ContentForge.IntegrationTests` | HTTP endpoints, auth, persistence, migrations, transactions |
| Architecture | `ContentForge.ArchitectureTests` | Any structural/layer change |
| Frontend unit | `frontend/.../tests/unit` | Stores, composables, utils |
| Frontend component | `frontend/.../tests/component` | Dynamic forms, guards, critical components |
| E2E | `frontend/.../e2e` | Critical user journeys (Phase 16+) |

### 4.3 Coverage Expectations

Minimum required coverage areas (SPEC §60–64):

- Domain: lifecycle, validation, permissions, slug, versioning, publishing, concurrency
- Integration: auth, authorization, CRUD, error responses, security scenarios
- E2E: login/logout, content workflow, role restrictions, media upload

### 4.4 Test Data

- Use factories/builders for complex objects
- Never depend on production data, developer machine state, or test execution order
- Integration tests use real PostgreSQL (Docker/Testcontainers)
- Clean up or use isolated databases per test collection

### 4.5 Test Naming

```text
MethodName_StateUnderTest_ExpectedBehavior
```

Example: `PublishContent_WhenDraftAndValid_TransitionsToPublishedAndCreatesVersion`

### 4.6 Definition of Done (Testing)

A feature is not complete until (SPEC §106):

1. Domain/application behavior is tested
2. API behavior is tested
3. Authorization is tested
4. Relevant frontend behavior is tested
5. Error cases are covered

---

## 5. Error Handling Conventions

### 5.1 Backend

- Domain throws typed exceptions (`DomainException`, `NotFoundException`, `ConflictException`, `ForbiddenException`)
- Application catches and translates or lets middleware map to HTTP status
- API uses global exception middleware producing **RFC 7807 Problem Details**
- Status code mapping:

| Condition | Status |
|-----------|--------|
| Validation failure | 422 Unprocessable Entity |
| Not found | 404 Not Found |
| Unauthorized | 401 Unauthorized |
| Forbidden | 403 Forbidden |
| Concurrency conflict | 409 Conflict |
| Bad input / unsupported filter | 400 Bad Request |
| Rate limited | 429 Too Many Requests |

- **Never** expose stack traces, internal paths, or SQL details in Production responses
- Concurrency 409 responses include enough context for frontend conflict resolution (current version, updated timestamp)

### 5.2 Frontend

- Centralized error handling in API client (`src/api/client.ts`)
- Map Problem Details to user-friendly messages
- Display field-level validation errors inline in forms
- Global toast/alert for unexpected errors
- Loading and error states required for all async operations

### 5.3 Validation Consistency

- **Backend is authoritative** (SPEC §104)
- Domain → Application → API validation pipeline
- Frontend validation mirrors backend for UX only
- Structured machine-readable validation errors with field keys matching API contract

---

## 6. Logging Conventions

### 6.1 Structured Logging

Use structured logging (Serilog or built-in JSON):

```json
{
  "timestamp": "...",
  "level": "Information",
  "message": "Content published",
  "service": "ContentForge.Api",
  "environment": "Production",
  "traceId": "...",
  "correlationId": "...",
  "userId": "...",
  "contentEntryId": "..."
}
```

### 6.2 Log Levels

| Level | Use |
|-------|-----|
| Debug | Development diagnostics only |
| Information | Normal operations (login success, publish, upload) |
| Warning | Recoverable issues (retry, validation rejection) |
| Error | Failures requiring attention (unhandled exceptions) |
| Critical | System-level failures |

### 6.3 Never Log

- Passwords or password hashes
- Access tokens or refresh tokens
- Connection strings or secrets
- Full request bodies containing credentials
- Sensitive personal data beyond user ID

### 6.4 Correlation

- Every request gets a correlation/trace ID
- Accept from trusted incoming header or generate
- Include in logs and error responses (`traceId` field)

---

## 7. Migration Rules

### 7.1 EF Core Migrations

- All schema changes via EF Core migrations — **never** manual Production DDL
- Migrations committed to source control with meaningful names
- Migrations must be deterministic and reviewed in PRs
- Test migrations in CI against clean and existing databases

### 7.2 Migration Authoring

- One logical change per migration where practical
- Include indexes and constraints in the same migration as the schema change they support
- Document breaking migrations in PR description
- Avoid data loss without explicit migration script and approval

### 7.3 Deployment

- Production migrations applied via **documented, controlled pipeline step** (SPEC §78)
- Do not rely on automatic `Database.Migrate()` on app startup in Production unless explicitly approved in ADR
- Staging must receive migrations before Production
- Rollback strategy documented in `docs/operations/`

### 7.4 Provider Compatibility

- Migrations tested against PostgreSQL (Development/CI)
- Validate Azure SQL compatibility before Production deployment
- Isolate provider-specific SQL in Infrastructure; avoid raw SQL in Domain/Application

---

## 8. API Conventions

### 8.1 Versioning

- All endpoints under `/api/v1/`
- Breaking changes require new version (`/api/v2/`)
- Public and admin routes clearly separated (see ARCHITECTURE.md §12)

### 8.2 HTTP Semantics

| Method | Usage |
|--------|-------|
| GET | Retrieve resource(s) |
| POST | Create resource or execute command (publish, submit) |
| PUT | Full replacement |
| PATCH | Partial update where supported |
| DELETE | Delete (soft delete where applicable) |

### 8.3 Pagination

Query parameters: `page`, `pageSize`

Response envelope:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 100,
  "totalPages": 5
}
```

- Enforce maximum `pageSize` server-side
- Default page size documented in OpenAPI

### 8.4 Sorting & Filtering

- Sort: `sortBy`, `sortDirection` — **whitelist only**
- Filter: explicit query parameters — **reject unknown filters with 400**
- Never accept arbitrary SQL/order expressions from clients

### 8.5 Response Conventions

- POST create → 201 Created with `Location` header
- DELETE → 204 No Content (or 200 with confirmation body if soft delete returns state)
- Commands with no body → appropriate 200/204

### 8.6 OpenAPI

- All endpoints documented with request/response models, status codes, auth requirements
- Validation error schema documented
- Swagger UI enabled in Development only (configurable for Staging)
- OpenAPI spec suitable for TypeScript client generation

---

## 9. Git & Pull Request Conventions

### 9.1 Commits

- Small, logically grouped, descriptive
- Preferred format: `type(scope): description`

```text
feat(content): add content lifecycle management
feat(auth): implement JWT authentication
test(content): add publishing integration tests
docs(architecture): document persistence strategy
```

Types: `feat`, `fix`, `test`, `refactor`, `docs`, `chore`, `ci`

### 9.2 Pull Requests

Significant changes require PR with:

- Summary
- Changes
- Tests added/run
- Architecture impact
- Security impact
- Migration impact (if any)
- Deployment impact (if any)

PRs must not merge when required CI checks fail.

### 9.3 Branching

- Feature branches from `main` (or `develop` if adopted — document in ADR)
- No force push to main/master

---

## 10. Security Development Rules

- Authorization on **every** protected mutation — no exceptions
- Input validation at API boundary AND domain/application for business rules
- Parameterized queries via EF Core — no string-concatenated SQL
- File uploads: validate type, size, extension; system-generated storage paths
- Rich text: sanitize before render in frontend
- Secrets in User Secrets / `.env` (local) or Azure Key Vault (Production) — never in Git
- CORS: explicit allowlist in Production; no wildcard origins
- Rate limiting on auth and public endpoints
- Security integration tests for: unauthorized access, privilege escalation, invalid JWT, disabled users, malformed input, invalid uploads

---

## 11. Frontend Development Rules

- API access **only** through `src/api/` modules
- Authentication headers and token refresh centralized in API client
- Route guards enforce authentication; permission checks for route access
- Permission-based UI visibility (hide/disable) — not a security substitute
- Unsaved changes detection in editors where practical
- Destructive actions require confirmation dialogs
- Publishing requires explicit user action
- WCAG 2.1 AA: keyboard navigation, focus states, semantic HTML, labels, accessible errors, contrast
- Loading states for all async operations
- No secrets in frontend source code

---

## 12. Docker & Local Development

- `docker compose up` must start PostgreSQL and Azurite
- Backend: `dotnet run`; Frontend: `npm run dev` on host for daily development
- Use `docker-compose.test.yml` for CI integration tests
- Production Dockerfile: multi-stage, non-root, env-var configuration

---

## 13. CI Verification Commands

GitHub Actions runs the full pipeline on pull requests and pushes to `main`. See **[docs/operations/ci.md](./operations/ci.md)** for job layout, quality gates, and local reproduction commands.

Before declaring any phase or feature complete, run the same checks locally:

**Backend:**

```bash
dotnet restore
dotnet build --configuration Release
dotnet format --verify-no-changes
dotnet test --configuration Release --no-build
```

**Frontend:**

```bash
cd frontend/contentforge-web
npm ci
npm audit --audit-level=moderate
npm run format:check
npm run lint
npm run typecheck
npm run test
npm run build:vite
```

**Infrastructure (when touching Bicep or Dockerfiles):**

```bash
bicep build infra/azure/bicep/main.bicep
docker build -f infra/docker/api/Dockerfile .
docker build -f infra/docker/web/Dockerfile .
```

---

## 14. AI Agent Rules

ContentForge is developed primarily with AI coding agents. These rules are mandatory for all agents.

### 14.1 Operating Principles (SPEC §108)

Agents must:

- Inspect existing code before modifying it
- Understand and respect dependency boundaries
- Reuse existing abstractions
- Avoid unnecessary rewrites
- Preserve working functionality
- Implement tests with behavior changes
- Run relevant validation commands
- Report failures honestly
- Avoid speculative features outside current phase scope
- Avoid unnecessary dependencies

### 14.2 Incremental Development (SPEC §109)

- Proceed in small, logically isolated increments
- Each increment: clear objective, minimum file changes, tests, validation, no regressions
- Large uncontrolled rewrites prohibited unless explicitly requested

### 14.3 No Fake Completion (SPEC §112)

Agents must **never**:

- Skip tests without reporting it
- Replace failing tests with weaker tests
- Disable analyzers to pass CI
- Suppress warnings without justification
- Fake external service responses as production implementations
- Leave TODO placeholders for required functionality
- Claim Azure deployment without verifying
- Claim security compliance without testing
- Declare a phase complete without running verification commands

### 14.4 Conflict Resolution

When spec and existing code conflict:

1. Stop and report the conflict
2. Do not silently change spec requirements
3. Propose resolution; wait for approval on architectural changes

### 14.5 Scope Discipline

- Implement only what the current task/phase requires
- Do not add features from future phases unless explicitly requested
- Do not invent requirements contradicting SPECIFICATIONS.md

### 14.6 Code Review (SPEC §110)

After every major subsystem, perform review checking:

- Architecture compliance
- Security
- Correctness
- Test coverage
- Performance considerations
- Maintainability
- Specification compliance

Reviews must produce **concrete findings**, not generic approval.

### 14.7 File Change Discipline

- Modify only files required for the task
- Do not refactor unrelated code
- Do not create documentation files unless requested (these three docs are explicitly requested)
- Match existing code style and patterns

### 14.8 Migration & Infrastructure Changes

- Include EF migrations with any schema change
- Update Docker/CI configuration when infrastructure changes
- Document deployment impact in PR/commit message

---

## 15. Code Quality Gates

Enforced via CI and local development:

- Nullable reference types
- Roslyn analyzers (warnings as errors where configured)
- `dotnet format --verify-no-changes`
- Frontend ESLint
- Architecture tests
- Dependency vulnerability scanning (CI)
- No critical accessibility violations before production release

---

## 16. Release & Versioning

- Application uses semantic versioning: `MAJOR.MINOR.PATCH`
- Breaking API changes → new API version or major bump
- Production release requires all items in SPEC §105 Production Readiness checklist

---

## 17. Related Documents

- [SPECIFICATIONS.md](../SPECIFICATIONS.md)
- [ARCHITECTURE.md](./ARCHITECTURE.md)
- [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)
- `docs/decisions/` — Architecture Decision Records
