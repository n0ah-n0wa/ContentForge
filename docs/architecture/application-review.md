# ContentForge Application Layer — Architecture Review

**Date:** 2026-08-29  
**Reviewer role:** Senior .NET architect  
**Scope:** `src/ContentForge.Application/`  
**Authority:** [SPECIFICATIONS.md](../../SPECIFICATIONS.md), [ARCHITECTURE.md](../ARCHITECTURE.md)

---

## Executive Summary

The Application layer implements a **hand-rolled CQRS** model: commands and queries as records, one handler per use case, FluentValidation for input shape, repository ports for persistence, and `ApplicationGuard` for authorization and domain-exception translation. Domain aggregates are orchestrated rather than duplicated for content lifecycle, validation, and versioning.

The layer **respects Clean Architecture boundaries** (no ASP.NET Core, EF Core, Azure, or HTTP types). Architecture tests enforce forbidden dependencies.

This review identified **11 concrete defects** spanning security, audit completeness, concurrency, and spec alignment. All were **fixed in this pass**. Remaining items are documented as deferred work with rationale.

**Verification after fixes:**

| Check | Result |
|-------|--------|
| `dotnet build --configuration Release` | 0 warnings, 0 errors |
| Unit tests | 78 passed |
| Architecture tests | 26 passed |
| Integration tests | 1 passed |

---

## 1. Use-Case Boundaries

### Strengths

- Each handler maps to a single administrative or public use case (39 handlers across auth, content types, content, lifecycle, versions, media, users, audit, public content, search).
- Handlers follow a consistent flow: authenticate → authorize → load aggregate → invoke domain → persist → audit.
- Public content queries are isolated under `PublicContent/` with no authentication requirement, matching SPEC §24.

### Issues Found & Fixed

| Issue | Spec reference | Fix |
|-------|----------------|-----|
| `CreateUserCommand` accepted password but never hashed or persisted it | §29 (secure password hashing) | Added `IPasswordHasher`; `UserAccount` now stores `PasswordHash`; handler hashes before persist |
| `LoginCommandHandler` dropped JWT tokens from `AuthenticationResult` | §29 (JWT access tokens) | Returns `LoginResultDto` including access/refresh tokens |
| `IContentSearchService` defined but unused | §20 (search) | Added `SearchContentQueryHandler` |

### Deferred

- **Manual handler registration** (39 entries in `DependencyInjection.cs`) will not scale. Acceptable for current phase; consider source-generated or convention-based registration before API surface grows.
- **`UserAccount` lives in Application**, not Domain. SPEC §7.1 allows Identity-backed implementation; a Domain `User` aggregate can be introduced when Identity integration lands (Phase 2).

---

## 2. Command / Query Separation

### Strengths

- Clear folder split: `Commands/` (mutate + `SaveChangesAsync`) vs `Queries/` (read-only).
- Public read API separated from admin queries.
- Criteria records co-located with queries (`ContentEntryListCriteria`, `AuditLogListCriteria`, etc.).

### Assessment

Structural CQRS is **sound**. Queries do not call `IUnitOfWork`. Commands do not return paginated collections except where the mutated aggregate is returned as a DTO (appropriate).

No MediatR pipeline — intentional lightweight approach. Acceptable at current scale.

---

## 3. DTO Design

### Strengths

- Output DTOs are immutable `sealed record` types (`ContentEntryDto`, `PublicContentDto`, etc.).
- Admin DTOs expose concurrency tokens and draft/published data separately, matching SPEC §12.
- `PublicContentDto` exposes only published field data — no audit, user, or internal metadata (SPEC §24).
- `UserDto` excludes `PasswordHash` (SPEC §29 — passwords never exposed).

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| Login returned `AuthenticatedUserDto` without tokens | `LoginResultDto` added with full token payload |
| `UserAccount` lacked `PasswordHash` | Added; excluded from `UserDto` mapping |

### Notes

- `FieldConfigurationDto` serves both command input and query output. Mapping to domain configuration remains in the handler; acceptable until a dedicated input record is needed.

---

## 4. Validation

### Three layers (correct per SPEC §16, §2310)

1. **FluentValidation** — structural input (email format, password length ≥ 12, non-empty slugs, concurrency version > 0).
2. **Application rules** — uniqueness checks (slug, email, content type name) → `ApplicationValidationException` (422).
3. **Domain rules** — schema validation, lifecycle transitions → domain exceptions translated by `ApplicationGuard`.

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| `Slug.Create` / `FieldName.Create` called outside exception translator, leaking `DomainValidationException` | Added `ApplicationGuard.CreateSlug()` / `CreateFieldName()` |
| `AddContentTypeFieldCommand` had no FluentValidation | Added `AddContentTypeFieldCommandValidator` |
| `DeleteContentEntryCommand` lacked concurrency validation | Added `DeleteContentEntryCommandValidator` |

### Deferred

- Lifecycle commands (submit, archive, unpublish) lack dedicated FluentValidation for concurrency version. Domain enforces token match; adding validators would improve fail-fast behavior.

---

## 5. Dependency Inversion

### Ports (Application abstractions)

| Port | Purpose | Justified |
|------|---------|-----------|
| `ICurrentUserService` | Auth context from Infrastructure | Yes |
| `IAuthenticationService` | Identity/JWT delegation | Yes |
| `IPasswordHasher` | Hashing algorithm isolation | Yes (added in review) |
| `IAuditService` | Side-effect recording | Yes |
| `IUnitOfWork` | Transaction boundary | Yes |
| `IDateTimeProvider` | Testability | Yes |
| `IFileStorage` | Blob storage | Yes |
| `IContentSearchService` | Admin search | Yes (now wired) |
| `I*Repository` | Persistence | Yes |

### Assessment

No infrastructure types leak through application contracts. Repository interfaces accept Application-layer criteria types — a pragmatic read-model choice that keeps query shapes co-located with handlers. Infrastructure implements ports via stubs until Phase 1 persistence.

**No overly generic interfaces detected.** Each port has a focused surface.

---

## 6. Domain / Application Responsibility Boundaries

### Correct delegation

- Content lifecycle transitions → domain aggregate methods
- Schema validation → `ContentDataValidator`
- Permission checks → `AuthorizationRules` (domain) invoked via `ApplicationGuard` (application)
- Version comparison → `ContentVersionComparer` (domain)

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| Authors could read other users' version history | `EnsureCanReadContent()` added; applied to all version queries |
| Author read scoping duplicated inline in `GetContentEntryQueryHandler` | Centralized in `ApplicationGuard.EnsureCanReadContent()` |

### Acceptable application-layer logic

- Slug/email uniqueness pre-checks (UX optimization; DB constraints remain authoritative in Infrastructure).
- Author list scoping via `criteria with { AuthorId = userId }` (server-side enforcement, SPEC §8 Author role).

---

## 7. Error Handling

### Exception taxonomy

| Application exception | Maps to (API layer) | Spec |
|----------------------|---------------------|------|
| `UnauthorizedApplicationException` | 401 | §29 |
| `ForbiddenApplicationException` | 403 | §30 |
| `NotFoundApplicationException` | 404 | §27 |
| `ConcurrencyConflictApplicationException` | 409 | §14 |
| `ApplicationValidationException` | 422 | §16, §28 |
| `UnsupportedQueryParameterException` | 400 | §23 |

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| Missing authentication threw `ForbiddenApplicationException` (403) | New `UnauthorizedApplicationException` (401) |
| Domain value-object validation bypassed translator | `CreateSlug` / `CreateFieldName` wrappers |

---

## 8. Pagination, Filtering, Sorting

### Implementation (SPEC §21–§23)

- **Pagination:** `PaginationRequest` with server-side clamping (1–100 page size); `PaginatedResult<T>` envelope with `items`, `page`, `pageSize`, `totalItems`, `totalPages`.
- **Sorting:** `SortRequest` with per-resource `AllowedSortFields` whitelists; unsupported sort → 400.
- **Filtering:** `FilterValidator.EnsureAllowed()` rejects unsupported filters; no silent ignore (SPEC §23).

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| Public list applied in-memory filter after DB pagination, inflating `totalItems` | Removed redundant filter; repository criteria already restrict to `ContentStatus.Published` |
| Static factory class named `PaginatedResult` collided with record name | Renamed to `PaginatedResults` |

---

## 9. Concurrency Handling

### Implementation (SPEC §14)

- Mutating commands carry `ConcurrencyRequest(uint Version)`.
- `ApplicationGuard.ToDomainToken()` translates to domain `ConcurrencyToken`.
- Domain `ConcurrencyConflictException` → `ConcurrencyConflictApplicationException` with expected/actual versions.

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| `DeleteContentEntryCommand` had no optimistic concurrency check | Added `ConcurrencyRequest`; handler validates token before soft delete |

### Deferred

- Domain `SoftDelete` does not accept a concurrency token internally. Application performs pre-check; consider extending the aggregate in a future domain pass for consistency with other mutating methods.

---

## 10. Authorization Abstractions

### Implementation (SPEC §8, §30)

- Permission strings defined in domain `Permissions` class.
- `ApplicationGuard.EnsurePermission()` delegates to `AuthorizationRules.EnsureAllowed()`.
- Author ownership enforced via `EnsureCanModifyContent()` (writes) and `EnsureCanReadContent()` (reads).
- Author list queries force `AuthorId = currentUser` server-side.

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| Version read queries lacked author scoping | `EnsureCanReadContent` on list/get/compare version handlers |
| `DisableUserCommand` allowed self-disable | Validation rejects disabling own account |

---

## 11. Audit Completeness (SPEC §32)

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| Soft delete recorded as `ContentUpdated` with metadata | New `AuditAction.ContentDeleted`; handler uses it |
| Withdraw from review not audited | Added audit with `AuditAction.ContentWithdrawnFromReview` |
| Missing domain audit actions | Added `ContentDeleted`, `ContentWithdrawnFromReview` to `AuditAction` enum |

---

## 12. Media Integrity (SPEC §18–§19)

### Issues Found & Fixed

| Issue | Fix |
|-------|-----|
| Upload wrote to storage before DB commit (orphan files on failure) | Persist metadata first, upload second, update URL; rollback marks asset deleted on upload failure |
| Delete removed storage before DB commit (orphan DB records on save failure) | Mark deleted + save first, then delete storage binary |

---

## 13. Spec Compliance Matrix

| SPEC requirement | Status |
|------------------|--------|
| §8 Permission-based authorization | Implemented |
| §14 Optimistic concurrency | Implemented (all mutating entry commands including delete) |
| §16 Dynamic content validation | Delegated to domain |
| §20 Admin search | Handler added; Infrastructure stub pending |
| §21–§23 Pagination/filter/sort | Implemented with whitelists |
| §24 Public API (published only) | Implemented |
| §29 Password hashing + JWT | Password hashing port added; login returns tokens |
| §32 Audit trail | Gaps fixed |
| §30 Server-side authorization | Implemented; not delegated to frontend |

---

## 14. Remaining Recommendations (Not Fixed — Out of Scope)

1. **Introduce Domain `User` aggregate** when Identity integration begins (Phase 2).
2. **Add FluentValidation** to remaining lifecycle commands for concurrency version.
3. **Extend domain `SoftDelete`** to accept concurrency token for consistency.
4. **Consider MediatR or source-generated registration** when handler count exceeds ~50.
5. **Batch-load entries in `SearchContentQueryHandler`** (N+1 per search result ID) when Infrastructure search is implemented.
6. **API exception middleware** (Phase 10) to map application exceptions to HTTP status codes.

---

## 15. Files Changed in Review Pass

| Area | Files |
|------|-------|
| Guard & exceptions | `ApplicationGuard.cs`, `UnauthorizedApplicationException.cs` |
| Auth | `AuthCommands.cs`, `AuthModels.cs` |
| Users | `UserCommands.cs`, `UserModels.cs`, `IPasswordHasher.cs` |
| Content | `ContentEntryCommands.cs`, `ContentLifecycleCommands.cs`, `ContentQueries.cs` |
| Content types | `ContentTypeCommands.cs` |
| Media | `MediaCommands.cs` |
| Public | `PublicContentQueries.cs` |
| Domain | `AuditLogEntry.cs` (new audit actions) |
| Infrastructure | `DependencyInjectionStubs.cs` |
| Tests | `AuthAndValidatorTests.cs`, `UserHandlerTests.cs` |

---

## Verdict

The Application layer is **architecturally sound** for Phase 1: clear use-case boundaries, proper dependency inversion, spec-aligned pagination/filtering, and domain orchestration without rule duplication. The review fixes addressed **security (password hashing, token return, author scoping), audit completeness, concurrency on delete, media transaction ordering, and exception semantics**.

The layer is ready for Infrastructure persistence implementations and API controller wiring in subsequent phases.
