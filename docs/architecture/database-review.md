# ContentForge Database & EF Core Architecture Review

**Date:** 2026-08-29  
**Scope:** `ContentForge.Infrastructure/Persistence`, EF Core migrations, repository query patterns  
**Authority:** [ADR-002](../decisions/ADR-002-database-strategy.md), [SPECIFICATIONS.md](../../SPECIFICATIONS.md)

---

## Executive Summary

The persistence layer follows Clean Architecture boundaries: EF Core entities, fluent configurations, repository implementations, and JSON serialization live in Infrastructure. PostgreSQL is used for development, CI, and integration tests; Azure SQL Database is the production target per ADR-002.

This review inspected entity mappings, indexes, constraints, cascade behavior, migrations, query performance, concurrency, transactions, JSON storage, soft deletion, and version immutability. **Six concrete issues were fixed** in this pass. Remaining items are documented as deferred work with deployment guidance for Azure SQL.

**Verification after fixes:**

| Check | Result |
|-------|--------|
| `dotnet build --configuration Release` | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | Passed |
| Unit tests | 78 passed |
| Architecture tests | 26 passed |
| Integration tests | 30 passed |

---

## 1. Entity Mappings

### Strengths

- One `IEntityTypeConfiguration<T>` per aggregate/table; applied via `ApplyConfigurationsFromAssembly`.
- Domain ↔ persistence mapping isolated in `DomainMappers.cs` with explicit `Restore`/`ToEntity`/`UpdateEntity` methods.
- String enums stored as `varchar` with bounded lengths (Status, FieldType, Action).
- JSON payloads (`DraftDataJson`, `PublishedSnapshotJson`, `ConfigurationJson`, `SnapshotJson`, `Metadata`) stored as `text` and serialized/deserialized through `PersistenceJsonConverter` — provider-neutral.
- `ConcurrencyToken` mapped as `bigint` with `.IsConcurrencyToken()` for optimistic concurrency.

### Issues Found & Fixed

| Issue | Risk | Fix |
|-------|------|-----|
| `EF.Functions.ILike` used in five repositories/services | **Breaks on Azure SQL** — `ILike` is Npgsql-only | Added `EfSearchExpressions` with provider branching (`ILike` vs `Like`) |
| Filtered unique index filter hardcoded to PostgreSQL syntax in configuration | **Breaks on Azure SQL** when model is applied with SqlServer provider | Moved filter to `AppDbContext.ConfigureProviderSpecificIndexes` using `DatabaseIndexFilters` |
| `ListAsync` eagerly loaded all `Versions` for every row | **N+1 payload bloat** on paginated lists | Added `ToDomainSummary` mapper; list queries no longer `Include(Versions)` |
| Per-child `AnyAsync` loop when marking new fields/versions | **N+1 round-trips** on updates | Batch `Contains` query for existing IDs |

### Observations (No Change Required)

- Domain `ConcurrencyToken` is `uint`; persistence column is `long`. Mapping uses `checked((uint))` cast. Safe for expected token ranges; document if tokens ever exceed `uint.MaxValue`.
- `ContentEntryRelationEntity` navigation properties (`OutgoingRelations`, `IncomingRelations`) exist but are not loaded in repositories — relations managed at application layer when needed.

---

## 2. Indexes

### Index Inventory vs Query Patterns

| Table | Index | Query Pattern | Assessment |
|-------|-------|---------------|------------|
| `ContentEntries` | `(ContentTypeId, Slug)` UNIQUE filtered | Slug lookup scoped to type; soft-delete slug reuse | **Correct** |
| `ContentEntries` | `(ContentTypeId, IsDeleted, UpdatedAt)` | Paginated list filtered by type, excluding deleted, sorted by `updatedAt` | **Added** in `AddContentEntryListIndex` migration |
| `ContentEntries` | `ContentTypeId`, `Status`, `IsDeleted`, `CreatedBy`, `CreatedAt`, `UpdatedAt` | Individual filter/sort columns | **Adequate** for current scale |
| `ContentEntries` | `Slug` standalone | Rarely queried without `ContentTypeId` | **Redundant** — low cost; defer removal |
| `ContentVersions` | `(ContentEntryId, VersionNumber)` UNIQUE | Version lookup and append-only insert | **Correct** |
| `ContentTypeFields` | `(ContentTypeId, Name)` UNIQUE, `(ContentTypeId, SortOrder)` | Field definition CRUD | **Correct** |
| `ContentTypes` | `Name`, `Slug` UNIQUE | Global uniqueness | **Correct** |
| `ContentEntryRelations` | `(SourceEntryId, TargetEntryId, FieldName)` UNIQUE | Relation deduplication | **Correct** |
| `AuditLogs` | `Timestamp`, `UserId`, `Action`, `(EntityType, EntityId)` | Audit list filters | **Correct** |
| `Users` | `Email` UNIQUE | Login lookup | **Correct** |

### Deferred

- **JSON field search** (`DraftDataJson LIKE '%term%'`) cannot use indexes on either provider without computed columns or full-text search. Acceptable for admin search at current scale; revisit with FTS (PostgreSQL `tsvector` / SQL Server full-text index) when content volume grows.
- **Composite audit index** `(UserId, Timestamp)` would help user-scoped audit queries; defer until audit volume warrants it.

---

## 3. Foreign Keys & Cascade Behavior

| Relationship | On Delete | Rationale | Assessment |
|--------------|-----------|-----------|------------|
| `ContentEntries` → `ContentTypes` | **Restrict** | Prevent orphan entries; types with entries cannot be deleted | **Correct** |
| `ContentVersions` → `ContentEntries` | **Cascade** | Versions are owned; deleted with entry | **Correct** |
| `ContentTypeFields` → `ContentTypes` | **Cascade** | Fields are owned by type | **Correct** |
| `ContentEntryRelations` source → entry | **Cascade** | Outgoing relations removed with source | **Correct** |
| `ContentEntryRelations` target → entry | **Restrict** | Prevent deleting entries referenced by others | **Correct** |
| `UserRoles` user → user | **Cascade** | Join cleanup | **Correct** |
| `UserRoles` role → role | **Restrict** | Roles with users cannot be deleted | **Correct** |
| `RolePermissions` role → role | **Cascade** | Join cleanup | **Correct** |
| `RolePermissions` permission → permission | **Restrict** | Permissions in use cannot be deleted | **Correct** |

No cascade cycles or dangerous multi-hop cascades detected.

---

## 4. Uniqueness Constraints

| Constraint | Scope | Notes |
|------------|-------|-------|
| `(ContentTypeId, Slug)` filtered unique | Active entries only | Allows slug reuse after soft delete |
| `(ContentEntryId, VersionNumber)` unique | Per entry | Enforces monotonic version numbers |
| `(ContentTypeId, FieldName)` unique | Per type | Prevents duplicate field keys |
| `ContentTypes.Name`, `ContentTypes.Slug` | Global | Admin identifiers |
| `Users.Email` | Global | Normalized to lowercase in mapper |
| `(SourceEntryId, TargetEntryId, FieldName)` | Per relation | Prevents duplicate links |

All align with domain invariants and are covered by integration tests.

---

## 5. Migration Quality

### Current Migrations

| Migration | Purpose | Assessment |
|-----------|---------|------------|
| `20260829142143_InitialCreate` | Full schema + authorization seed data | **Correct for PostgreSQL** |
| `20260829144739_AddContentEntryListIndex` | Composite list query index | **Correct** |

### PostgreSQL-Specific Elements in `InitialCreate`

The initial migration was generated against Npgsql and contains provider-specific column types:

- `uuid`, `boolean`, `timestamp with time zone`, `character varying`, `text`, `bigint`
- Filtered index: `"IsDeleted" = false` (PostgreSQL boolean syntax)

**These will not apply directly to Azure SQL.** Per ADR-002, migrations are tested against PostgreSQL in CI and must be validated/regenerated for production deployment.

### Azure SQL Deployment Checklist

Before applying to Azure SQL:

1. Regenerate or translate migrations using `UseSqlServer` design-time factory (`Database:Provider=SqlServer`).
2. Verify filtered index filter becomes `[IsDeleted] = 0`.
3. Confirm `DateTimeOffset` maps to `datetimeoffset` (supported on both providers via EF Core).
4. Run integration tests against Azure SQL or LocalDB before production cutover.
5. Seed data in `InitialCreate` uses GUID literals — compatible with both providers.

### Seed Data

Authorization roles and permissions are embedded in `InitialCreate` via `InsertData`. Idempotent re-application is handled by EF migration history; do not re-run seed outside migrations.

---

## 6. Query Performance & N+1 Risks

### Strengths

- Read paths use `AsNoTracking()` consistently.
- Pagination uses `LongCountAsync` + `Skip`/`Take` via `QueryExtensions.ToPaginatedResultAsync`.
- `EfContentSearchService` projects to `Guid` IDs only — avoids loading JSON payloads for search results.

### Issues Found & Fixed

| Location | Issue | Fix |
|----------|-------|-----|
| `EfContentEntryRepository.ListAsync` | `Include(Versions)` on every list row | Removed; use `ToDomainSummary` |
| `MarkNewVersionsAsAddedAsync` | N × `AnyAsync` | Single batch `Contains` query |
| `MarkNewFieldsAsAddedAsync` | N × `AnyAsync` | Single batch `Contains` query |

### Remaining N+1 Risks (Acceptable)

| Location | Pattern | Mitigation |
|----------|---------|------------|
| `EfUserRepository.ListAsync` | `Include(UserRoles).ThenInclude(Role)` | Users are low-volume admin entities |
| `EfContentTypeRepository.ListAsync` | `Include(Fields)` | Content types are low-volume; fields needed for domain restore |
| `EfContentEntryRepository.GetByIdAsync` | `Include(Versions)` | Required for aggregate restore |

---

## 7. Concurrency Configuration

### Design

- `ContentEntries.ConcurrencyToken` (`bigint`, `.IsConcurrencyToken()`) incremented by domain on every mutating operation.
- `EfUnitOfWork.SaveChangesAsync` catches `DbUpdateConcurrencyException` and rethrows `ConcurrencyConflictException`.
- Domain layer validates token before mutation (`EnsureConcurrency`).

### Observations

- `EfContentEntryRepository.UpdateAsync` reloads fresh entity from DB (`ChangeTracker.Clear()` + load). Stale client tokens are detected at domain layer before persistence; EF token check is a second line of defense.
- `ConcurrencyConflictException(0, 0)` from `EfUnitOfWork` discards actual token values. Low impact — API layer can reload entry; improve if client-facing error detail is needed.

Integration tests verify both domain-level and persistence-level conflict detection.

---

## 8. Transaction Boundaries

- `IUnitOfWork` scoped per request/test scope (`AddScoped`).
- Repositories do not call `SaveChanges` — handlers orchestrate unit of work commits.
- No explicit `BeginTransaction` usage yet; single `SaveChangesAsync` per command is sufficient for current use cases.
- Integration tests verify rollback on failure within a scope.

### Deferred

- Multi-repository operations requiring explicit transactions (e.g., content + audit atomicity under failure) should use `IUnitOfWork` transaction API when added.

---

## 9. PostgreSQL / Azure SQL Compatibility

| Feature | PostgreSQL (dev/CI) | Azure SQL (prod) | Status |
|---------|---------------------|------------------|--------|
| Case-insensitive search | `ILike` | `Like` (CI collation) | **Fixed** — `EfSearchExpressions` |
| Filtered unique index | `"IsDeleted" = false` | `[IsDeleted] = 0` | **Fixed** in model; migration translation required for prod |
| JSON storage | `text` + app serialization | `nvarchar(max)` + app serialization | **Portable** |
| UUID primary keys | `uuid` | `uniqueidentifier` | EF abstracts; migration regen required |
| Date/time | `timestamptz` | `datetimeoffset` | **Portable** via EF |
| Boolean | `boolean` | `bit` | EF abstracts; migration regen required |
| Full-text / JSON operators | Not used yet | Not used yet | **Portable** |

Provider selection via `DatabaseOptions.Provider` (`PostgreSQL`, `SqlServer`, `AzureSQL`) in `DesignTimeDbContextFactory.ConfigureProvider`.

---

## 10. JSON Storage Strategy

### Approach

Dynamic field values, field configurations, content snapshots, and audit metadata are stored as **serialized JSON strings** in `text` columns:

| Column | Content |
|--------|---------|
| `ContentEntries.DraftDataJson` | Draft field values |
| `ContentEntries.PublishedSnapshotJson` | Published snapshot |
| `ContentTypeFields.ConfigurationJson` | Validation/relation config |
| `ContentVersions.SnapshotJson` | Point-in-time snapshot |
| `AuditLogs.Metadata` | Optional audit context |

### Strengths

- Provider-neutral — no `jsonb` or `JSON` column types.
- `PersistenceJsonConverter.NormalizeJsonValue` handles `JsonElement` deserialization after DB round-trip.
- Domain validation runs on deserialized `ContentData`, not raw JSON.

### Limitations

- No database-level JSON schema validation.
- Keyword search scans raw JSON text (`LIKE`/`ILIKE`) — not indexed.
- Large payloads increase row size; consider offloading media references (already separate `Media` table).

---

## 11. Soft Deletion

### Design

- `ContentEntries.IsDeleted` and `Media.IsDeleted` boolean flags.
- Queries default to `!IsDeleted` unless `IncludeDeleted` is specified.
- Slug uniqueness enforced only for active entries via filtered unique index.

### Strengths

- Slug reuse after soft delete works correctly (integration tested).
- No cascade delete on soft delete — relations and versions preserved for audit/recovery.

### Observations

- Soft-deleted rows remain in unique indexes that are not filtered (e.g., global `ContentTypes.Slug`). Types are not soft-deleted — acceptable.
- Hard delete of content types cascades to fields but is blocked by `Restrict` FK from entries.

---

## 12. Historical Version Immutability

### Design

- `ContentVersions` are append-only: new rows inserted on draft update/publish; existing rows never updated by domain mappers.
- `(ContentEntryId, VersionNumber)` unique constraint prevents duplicate version numbers.
- No update/delete API on `ContentVersion` domain type.

### Issue Found & Fixed

| Issue | Fix |
|-------|-----|
| No database-layer enforcement against direct EF modification of version rows | `AppDbContext.EnforceVersionImmutability()` rejects `Modified`/`Deleted` state on `ContentVersionEntity` at `SaveChanges` |

Integration test `ContentVersion_DirectModificationIsRejectedByDbContext` verifies the guard.

---

## 13. Files Changed in This Review

| File | Change |
|------|--------|
| `Persistence/EfSearchExpressions.cs` | Provider-portable search predicates |
| `Persistence/DatabaseIndexFilters.cs` | PostgreSQL/SQL Server filter expressions |
| `Persistence/AppDbContext.cs` | Provider-specific index filter; version immutability guard |
| `Configurations/ContentEntryEntityConfiguration.cs` | Composite list index; filter moved to DbContext |
| `Repositories/EfContentEntryRepository.cs` | Portable search; no version include on list; batch version ID check |
| `Repositories/EfContentTypeRepository.cs` | Portable search; batch field ID check |
| `Repositories/EfMediaRepository.cs` | Portable search |
| `Repositories/EfUserRepository.cs` | Portable search |
| `Services/EfContentSearchService.cs` | Portable search |
| `Mapping/DomainMappers.cs` | `ToDomainSummary` for list queries |
| `Migrations/20260829144739_AddContentEntryListIndex.cs` | New composite index |
| `tests/.../ContentVersionPersistenceTests.cs` | Version immutability guard test |

---

## 14. Recommended Next Steps

1. **Azure SQL validation pipeline** — add CI job or manual gate that applies migrations with `SqlServer` provider against LocalDB/Azure SQL edge.
2. **Full-text search** — replace JSON `LIKE` scans when content volume exceeds admin-scale.
3. **Explicit transactions** — extend `IUnitOfWork` with `BeginTransactionAsync` for multi-step commands.
4. **Concurrency error detail** — propagate expected/actual tokens from `DbUpdateConcurrencyException` to API responses.
5. **Index hygiene** — remove redundant `IX_ContentEntries_Slug` if query plans confirm it is unused.
