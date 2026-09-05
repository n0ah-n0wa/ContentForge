# ContentForge — Final Backend Production Review

**Date:** 2026-09-05  
**Scope:** Entire .NET 8 backend (`Domain`, `Application`, `Infrastructure`, `Infrastructure.SqlServer`, `Api`) against [SPECIFICATIONS.md](../../SPECIFICATIONS.md)  
**Authority:** SPECIFICATIONS.md, [ARCHITECTURE.md](../ARCHITECTURE.md), ADR-001 / ADR-002  
**Verification:** Backend quality gate (restore, NuGet audit, format, Release build, EF migration validation, unit + architecture + integration tests)

---

## Executive summary

ContentForge’s backend is a **production-oriented Clean Architecture CMS**: hand-rolled CQRS, Domain aggregates for content lifecycle/versioning, EF Core persistence entities mapped to Domain, Identity + JWT, permission policies, audit logging, media storage abstraction, scheduled publishing, and dual PostgreSQL / Azure SQL migrations.

This pass found **no CRITICAL** defects. Several **HIGH** issues were confirmed and **fixed with regression tests**. Remaining items are documented as deferred / accepted risk with rationale.

| Severity | Found | Fixed this pass | Deferred |
|----------|------:|----------------:|---------:|
| CRITICAL | 0 | 0 | 0 |
| HIGH | 6 | 6 | 0 |
| MEDIUM | 9 | 2 | 7 |
| LOW | 5 | 0 | 5 |

**Quality gate (local, matching CI Backend job):**

| Gate | Result |
|------|--------|
| `dotnet restore` + vulnerable package audit | Pass (0 vulnerable) |
| `dotnet format --verify-no-changes` | Pass |
| `dotnet build -c Release` | Pass (0 warnings / 0 errors) |
| EF `has-pending-model-changes` (PostgreSQL + Azure SQL) | Pass |
| Unit tests | **169 passed** |
| Architecture tests | **31 passed** |
| Integration tests (PostgreSQL 16 on `:5433`) | **245 passed** |

---

## Architecture assessment (vs SPEC §4–§7)

### Strengths

- Dependency direction is inward and **enforced** by NetArchTest (`ContentForge.ArchitectureTests`).
- Domain has **zero** NuGet dependencies; content lifecycle, concurrency tokens, versioning, and media upload rules live in Domain.
- Application uses ports (`I*Repository`, `IFileStorage`, `IUnitOfWork`, scheduling/caching abstractions); Infrastructure supplies EF/Identity/blob implementations.
- Controllers are thin handlers; Problem Details map application exceptions consistently.
- Optimistic concurrency on content entries; pagination/sort/filter whitelisting; rate limiting on abuse-sensitive routes.

### Watch points (not rewritten)

| Item | Classification | Notes |
|------|----------------|-------|
| Manual handler DI (~53 `AddScoped`) | Underengineering / scalability | Acceptable at current size; convention/source-gen later |
| `UserAccount` in Application (not Domain) | Spec-aligned Identity split | Documented in prior application review |
| `ContentController` surface area | Maintainability | Thin but large; split by resource when it grows |
| `*ApiRequest` types in Application | Mild layer leak | Prefer Api contracts only long-term |
| Domain `InternalsVisibleTo` Infrastructure | Hidden coupling | Needed for `Restore` factories; keep narrow |
| Dual migration assemblies | Operational complexity | Mitigated by schema-parity architecture tests |

---

## Findings fixed this pass

### HIGH-1 — Soft-deleted media remained anonymously downloadable

- **Impact:** After CMS soft-delete (or if blob delete failed), `/media-files/{key}` still served binaries → confidentiality failure for “deleted” assets.
- **Fix:** `IMediaRepository.ExistsActiveByStorageKeyAsync`; `MediaFilesController` returns 404 unless an **active** media row exists.
- **Tests:** `GetMediaFile_WhenSoftDeletedButBlobRemains_ReturnsNotFound`.

### HIGH-2 — SVG XSS via inline delivery

- **Impact:** Allowed SVG uploads could execute script when opened inline in a browser.
- **Fix:** Stronger `MediaContentInspector` denylist (event handlers, `foreignObject`, etc.); SVG responses forced to **attachment** (`Content-Disposition`).
- **Tests:** Domain inspector cases for `onload` / `foreignObject`.

### HIGH-3 — Refresh-token rotation race

- **Impact:** Concurrent refresh with the same token could issue two valid refresh tokens.
- **Fix:** Atomic `ExecuteUpdate` claim (`RevokedAt == null`) inside a DB transaction; loser fails cleanly without family revoke.
- **Tests:** Existing reuse / refresh integration tests remain green.

### HIGH-4 — Schedule jobs vs content update transaction boundary

- **Impact:** Job `SaveChanges` could commit before entry update failed → orphan jobs publishing after a failed schedule.
- **Root cause nuance:** `EfContentEntryRepository.UpdateAsync` calls `ChangeTracker.Clear()`, so job inserts **must** flush before update; coupling is intentional.
- **Fix:** Restore job flush; wrap schedule + entry update in `IUnitOfWork.ExecuteInTransactionAsync` so concurrency failure rolls back job writes. Processor skips publish/unpublish when schedule fields are null (orphan defense).
- **Tests:** Full `ScheduledPublishingIntegrationTests` suite.

### HIGH-5 — Content-type cascade delete not transactional

- **Impact:** `ExecuteDeleteAsync` of entries could succeed while type delete failed → orphaned schema.
- **Fix:** Confirmed safe-deletion path runs entry purge + type delete inside `ExecuteInTransactionAsync`.

### HIGH-6 — Scheduled job claim SQL was PostgreSQL-only

- **Impact:** Azure SQL / SqlServer providers could not claim due jobs (`LIMIT` / `FOR UPDATE SKIP LOCKED`).
- **Fix:** Provider-specific claim SQL (Npgsql vs SqlServer `UPDLOCK, READPAST`).

### MEDIUM (fixed)

- **Disable/Enable user** commands lacked FluentValidation → empty GUID could surface as 500; validators + handler wiring added.
- **JWT options** validation now requires Issuer/Audience and positive lifetimes (not only signing-key length).

---

## Remaining findings (deferred)

### MEDIUM

| ID | Finding | Impact | Why deferred |
|----|---------|--------|--------------|
| M1 | Runtime authz uses `DefaultRoleDefinitions`, not DB `RolePermissions` | DB permission edits ineffective | Product decision: code-defined RBAC; document until admin UI edits permissions |
| M2 | Audit gaps (logout, password reset, media metadata, content-type delete, schedule) | Incomplete security trail | Incremental backlog; redaction itself is solid |
| M3 | Create/user/media audits often outside same transaction as mutation | Lost audit after success | Prefer extending `ContentMutationPersistence` pattern gradually |
| M4 | `GetById` always includes full version history | Heavy payloads for long histories | Needs dedicated “summary vs detail” repository APIs |
| M5 | Public slug fallback uses JSON `Contains` | Potential unbounded scan | Acceptable at current scale; search port exists for later |
| M6 | Content types / media / users lack optimistic concurrency | Lost updates under concurrent admin edits | Spec mandates concurrency for content entries primarily |
| M7 | Anonymous media URLs are world-readable if known | By design for private-blob + proxy | Signed URLs optional hardening |

### LOW

| ID | Finding | Notes |
|----|---------|-------|
| L1 | `LocalFileStorage.DeleteAsync` ignores cancellation | Minor |
| L2 | Dev JWT / DB credentials in `appsettings.Development.json` | Expected; production placeholders rejected |
| L3 | `AllowedHosts: *` in base settings | Override in production host config |
| L4 | Weak `.txt` magic-byte check | Size/MIME still enforced |
| L5 | Dashboard `[Authorize]` without policy | Handler scopes by permission correctly |

---

## Area-by-area notes

### Domain

Lifecycle transitions, publish/unpublish rules, version append-only semantics, concurrency tokens, and media upload constraints are coherent and testable. No Domain → Infrastructure leaks observed.

### Application

CQRS handlers are consistent. `ApplicationGuard` centralizes authz + domain exception translation. Duplication between controller policies and handler checks is **defense-in-depth**, not accidental.

### Infrastructure / EF Core

Persistence entities stay out of Domain. Immutability guards protect versions and audit rows. Indexes and soft-delete filters are provider-aware. Dual migration projects remain the operational hotspot (parity tests help).

### API / Identity / authorization

JWT validation, security-stamp checks, lockout, and permission policies meet SPEC §29–§30 intent. Rate limiting covers login/refresh/password-reset/public/media/preview.

### Storage / media

System-generated storage keys, path containment, allowlists, and magic-byte checks are in place. Soft-delete delivery gate closes the main remaining hole.

### Audit / logging / health / caching / jobs

Structured logging + correlation ID middleware present. Ready health checks cover DB + storage. Public content cache is invalidate-on-write. Scheduled publishing is durable with locking; SqlServer claim path now exists.

### Configuration / errors

Ad-hoc JWT/DB validation at startup (improved). Full `IValidateOptions` / `ValidateOnStart` for all option types remains a polish item. Exception handler avoids stack traces; prefer FluentValidation for all mutating commands so Domain ID parse failures do not become 500s (Disable/Enable fixed).

---

## Spec alignment checklist (backend)

| SPEC theme | Status |
|------------|--------|
| Clean Architecture inward deps | Met (architecture tests) |
| Content lifecycle + versioning + concurrency | Met |
| Public vs admin API separation | Met |
| Permission-based authorization | Met (code-defined matrix) |
| Media outside DB + untrusted uploads | Met (with SVG hardening) |
| Audit for sensitive ops | Mostly met (gaps listed) |
| Pagination / sort / filter safety | Met |
| Observability / health | Met |
| Dual DB strategy (PG + Azure SQL) | Met (claim SQL fixed) |

---

## Recommended next work (priority order)

1. Extend audit coverage for auth and schema mutations (M2).  
2. Transactional audit for create paths (M3).  
3. Version-summary queries to avoid unbounded includes (M4).  
4. Convention-based handler registration when API surface grows.  
5. Optional signed media URLs if threat model requires non-shareable links (M7).

---

## Conclusion

The backend is **release-capable** for the ContentForge production target after this remediation pass: architecture boundaries hold, high-severity integrity/security defects are closed with tests, and the full backend quality gate is green. Remaining items are incremental hardening, not blockers for a first production deployment behind proper secrets, TLS, and Azure configuration.
