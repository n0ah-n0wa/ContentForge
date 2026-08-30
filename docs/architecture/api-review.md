# ContentForge REST API — Architecture Review

**Date:** 2026-08-30  
**Reviewer role:** Senior API architect  
**Scope:** `src/ContentForge.Api/`, API-facing DTOs in Application, JWT challenge/forbidden handling  
**Authority:** [SPECIFICATIONS.md](../../SPECIFICATIONS.md) §§24–28, [ARCHITECTURE.md](../ARCHITECTURE.md)

---

## Executive Summary

The HTTP surface is a versioned ASP.NET Core controller API (`/api/v1/...`) with a clean split between administrative routes (JWT + permission policies) and anonymous public content (`/api/v1/public/...`). Controllers are mostly thin command/query adapters. Errors use RFC 7807 Problem Details. Collection queries use a shared pagination envelope.

This review found **concrete defects** in DTO boundaries, query-parameter handling, authorization gaps around soft-delete, logout over-posting, OpenAPI completeness, and inconsistent 401/403 payloads. **All listed issues were fixed in this pass.** Remaining items are deferred with rationale.

**Verification after fixes:** see the test run recorded at the end of this document.

---

## 1. HTTP Semantics

### Strengths

- `GET` is read-only; `POST` creates or executes lifecycle commands; `PUT` replaces mutable fields; `DELETE` removes or soft-deletes.
- Creates return **201 Created** with `Location` via `CreatedAtAction`.
- Deletes that succeed return **204 No Content**.
- Lifecycle transitions (`publish`, `archive`, `disable`) correctly use `POST` as commands, not `PUT`.

### Issues found and fixed

| Issue | Spec | Fix |
|-------|------|-----|
| JWT 401/403 responses were not Problem Details | §28 | `JwtBearer` `OnChallenge` / `OnForbidden` write `application/problem+json` |
| Unhandled exceptions could leak implementation details | §28 | Catch-all handler returns generic 500 Problem Details (cancellations rethrown) |
| Validation and application errors mixed `application/json` vs Problem Details | §28 | `application/problem+json` on exception and model-state paths |

### Deferred

- **DELETE with a JSON body** (content entries, content-type field removal) is valid HTTP but awkward for some clients. Kept because concurrency tokens / confirmation flags must travel with the request. Query-string concurrency would be worse.

---

## 2. Route Design and Versioning

### Strengths

- All routes sit under `/api/v1/` (SPEC §26).
- Resource names match SPEC §25 (`auth`, `users`, `roles`, `content-types`, `content`, `media`, `audit`) and §24 (`public/{contentType}/{slug}`).
- Guid constraints on identity routes; integer constraints on version numbers; `versions/compare` cannot collide with `{versionNumber:int}`.

### Assessment

Versioning is **URI-based and consistent**. Breaking changes require `/api/v2/`. No silent unversioned public surface.

---

## 3. DTO Boundaries

### Strengths

- Request contracts are dedicated records (`CreateContentApiRequest`, `LogoutApiRequest`, etc.), not EF entities.
- `PublicContentDto` exposes only published snapshot fields.
- `UserDto` never includes `PasswordHash`.

### Issues found and fixed

| Issue | Risk | Fix |
|-------|------|-----|
| `POST /auth/refresh` returned `AuthenticationResult` containing domain `UserId` | Direct value-object / structure leak; inconsistent with login | Returns `LoginResultDto` (GUID `userId`) |
| `LogoutRequest` required `UserId` in the HTTP body | Over-posting / confused identity; `userId` omitted deserialized as `Guid.Empty` → 403 | `LogoutApiRequest` accepts only `refreshToken`; identity from the principal |
| `MediaAssetDto.StorageKey` exposed blob path layout | Internal storage structure leak | Removed from the API DTO |

System.Text.Json ignores undeclared JSON properties on request records, which blocks classic mass assignment onto entities (entities are never bound).

---

## 4. Problem Details, Validation, Status Codes

### Strengths

- FluentValidation mapped to **422** with `errors`.
- Unsupported sort/filter → **400**.
- Missing resources → **404**.
- Optimistic concurrency → **409**.
- Auth failures → **401** with a generic detail (no credential oracle beyond existing login audit).

### Issues found and fixed

| Issue | Fix |
|-------|------|
| Invalid enum query values (`status=not-a-status`) were silently dropped | `ListCriteriaFactory` rejects unknown enum values with 400 |
| Invalid `sortDirection` was coerced to `asc` | Rejected as an unsupported query parameter |
| Login/refresh validators existed but were not invoked | Handlers call `CommandValidator.EnsureValidAsync` |

---

## 5. Authorization and Public/Private Separation

### Strengths

- Controller `[Authorize(Policy = ...)]` plus handler `ApplicationGuard` (defense in depth).
- Public controller is `[AllowAnonymous]` and OpenAPI omits bearer for those operations.
- Public queries force `ContentStatus.Published` and hide inactive types.

### Issues found and fixed

| Issue | Risk | Fix |
|-------|------|-----|
| `includeDeleted=true` available to any `content.read` / `media.read` caller | Viewers could list soft-deleted content | Requires `content.delete` / `media.delete` |
| Soft-deleted media remained fetchable by id | Existence/content leak | 404 unless the caller can delete media |
| Version endpoints loaded deleted content entries | Inconsistent with GET content 404 | `RequireVisibleContentEntry` on version list/get/compare |
| Logout accepted a target `userId` | Confused ownership (handler already rejected mismatches) | User id no longer part of the HTTP contract |

Public vs admin remains a **route and DTO** split, not a filter on the admin model.

---

## 6. Pagination, Filtering, Sorting

### Strengths

- Envelope: `items`, `page`, `pageSize`, `totalItems`, `totalPages`.
- Page size clamped server-side (`PaginationDefaults.MaxPageSize`).
- Sort fields are allow-listed per resource.

### Issues found and fixed

| Issue | Spec | Fix |
|-------|------|-----|
| Admin lists **ignored** unknown query keys | §23 must reject unsupported filters | `QueryBinding.EnsureAllowedQueryParameters` on all collection endpoints |
| Query parameters were not on OpenAPI (bound from `Request.Query` only) | OpenAPI quality | `[FromQuery]` on list/search actions |

Public lists already rejected unknown filters; they now also reject invalid `sortDirection`.

---

## 7. Concurrency

### Strengths

- Mutating content operations require `concurrencyToken` in the body, mapped to domain tokens.
- 409 on mismatch.

### Deferred

- HTTP `If-Match` / ETag is not implemented. Body tokens match the current admin SPA contract and SPEC §12. Adding ETags would be a non-breaking complement later.

---

## 8. OpenAPI Quality

### Strengths

- Swagger v1 with XML comments, bearer scheme, anonymous public operations.
- Public endpoints tagged and described as published-only.
- List endpoints now declare pagination, sort, and filter parameters.

### Issues found and fixed

| Issue | Fix |
|-------|------|
| Class-level 401/403 missing on several admin controllers | Added `ProducesResponseType` on controller types |
| Refresh documented as `AuthenticationResult` | Documented as `LoginResultDto` |
| Enums serialized as integers in JSON | Documented; OpenAPI still lists named members. String enums need a coordinated client serializer and were not switched in this pass to avoid a silent contract break |

### Deferred

- Per-operation examples and a dedicated public-only OpenAPI document. Single v1 document remains correct while admin and public share the host.

---

## 9. Security

### Strengths

- JWT validation includes issuer, audience, lifetime, algorithm allow-list, and security-stamp checks for disabled users.
- CORS is origin-allow-list based when configured.
- Public API rate limiting (fixed window per IP); Testing uses a high limit so suites do not flake.

### Issues found and fixed

Covered above: logout identity, storage key leak, soft-delete visibility, Problem Details for auth failures, generic 500s.

### Deferred

- **429** was specified (SPEC §27) but unused until this pass’s limiter. Tuning (distributed counter, CDN) is an operations concern.
- Upload `Content-Type` is still taken from the client; file-type allow-listing lives in application/media rules and should stay there, not in the controller.

---

## 10. Controllers and Business Logic

Controllers map HTTP to commands/queries. Remaining controller-local checks are **transport validation** only (empty upload stream, query allow-lists). Logout no longer parses claims; `ICurrentUserService` owns identity.

---

## Deferred (non-blocking)

| Item | Rationale |
|------|-----------|
| ETag / `If-Match` | Body concurrency tokens already enforce lost-update protection |
| Split OpenAPI documents | One v1 spec is enough; tags separate public vs admin |
| DELETE without body | Would require an alternative concurrency channel |
| Stricter CORS methods | AllowAnyMethod is acceptable behind an origin allow-list for a JSON API |

---

## Verification

Run after this review (serial integration collection; shared PostgreSQL fixture races if tests run in parallel):

```bash
dotnet format --verify-no-changes
dotnet test -- xUnit.MaxParallelThreads=1
```

**2026-08-30 results**

| Check | Result |
|-------|--------|
| `dotnet format --verify-no-changes` | Passed |
| Unit tests | 98 passed |
| Architecture tests | 26 passed |
| Integration tests | 136 passed |
| Failed / skipped | 0 / 0 |
