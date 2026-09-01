# Database Migrations — Production Strategy

ContentForge uses **committed EF Core migrations** applied through an **explicit, controlled pipeline step**. Schema changes never run automatically on API startup in Staging or Production.

**Related:** [Azure CD](./azure-cd.md) · [Azure deployment](./azure-deployment.md) · [DEVELOPMENT_RULES.md §7](../DEVELOPMENT_RULES.md) · [SPECIFICATIONS.md §78](../../SPECIFICATIONS.md)

---

## Principles

| Principle | Implementation |
|-----------|----------------|
| Committed migrations only | All schema changes via EF Core migrations in source control |
| No startup migrations in deployed environments | `DevelopmentDatabaseInitializer` runs only when `IsDevelopment()` |
| No database drop/recreate | Pipeline refuses missing databases; never runs `database drop` |
| Staging before production | Production deploy requires manual confirmation that staging was validated |
| Explicit failure | Migration job failure **blocks** App Service deployment |
| Destructive changes reviewed | CI scans `Up()` methods; allowlist required for drops/deletes |
| Provider parity | PostgreSQL (dev/CI) and Azure SQL (cloud) snapshots must define the same tables |

---

## Migration projects

| Provider | Project | Used by |
|----------|---------|---------|
| PostgreSQL | `ContentForge.Infrastructure` | Local dev, Docker Compose, CI integration tests |
| Azure SQL | `ContentForge.Infrastructure.SqlServer` | Staging and Production deployments |

When you change the model, add migrations to **both** projects (or regenerate Azure SQL from the same model state):

```bash
# PostgreSQL (local/CI)
dotnet ef migrations add <Name> \
  --project src/ContentForge.Infrastructure \
  --startup-project src/ContentForge.Api

# Azure SQL (cloud)
dotnet ef migrations add <Name> \
  --project src/ContentForge.Infrastructure.SqlServer \
  --startup-project src/ContentForge.Api
```

---

## Pipeline flow

```text
PR / push
   ↓
validate-migrations.sh          ← architecture tests + has-pending-model-changes
   ↓
(deploy workflow)
   ↓
dry-run: list pending migrations
   ↓
apply: dotnet ef database update   ← only pending migrations, never drop DB
   ↓
verify: no (Pending) remain
   ↓
deploy App Service containers      ← blocked if migrate job failed
```

### Failure behavior

| Failure | Result |
|---------|--------|
| Pending model changes (no committed migration) | CI and deploy **validate** fail |
| Unapproved destructive `Up()` operation | Architecture / validate script **fail** |
| Target database does not exist | Migration script **exits 1** — provision via Bicep first |
| `dotnet ef database update` error | Migrate job **fails** — deploy job does not run |
| Pending migrations after update | Migrate job **fails** |
| Production without confirmations | Migrate job **fails** before connecting to SQL |

The API container is **not** updated until migrations succeed (deploy job `needs: migrate`).

---

## Staging validation

1. Every merge to `main` deploys **staging** and runs migrations against the staging Azure SQL database.
2. Verify application behavior in staging (UI, API via Web proxy, critical flows).
3. Production deploy (manual workflow) requires:
   - `confirm_staging_validated=true`
   - `confirm_production_migration=true`
   - GitHub **production** environment approval (reviewers)

**Never** skip migrations in production (`skip_migrations` is blocked for production).

---

## Validation tooling

### Local / CI script

```bash
./infra/azure/scripts/validate-migrations.sh
```

Runs:

1. `MigrationSafetyTests` and `MigrationSchemaParityTests` (architecture test project)
2. `dotnet ef migrations has-pending-model-changes` for each provider project with `Database__Provider` set to `PostgreSQL` or `AzureSQL` respectively

### Architecture tests

| Test | Purpose |
|------|---------|
| `MigrationUpMethods_ShouldNotContainUnapprovedDestructiveOperations` | Blocks `DropTable`, `DropColumn`, `DeleteData`, `DROP DATABASE`, etc. in `Up()` unless allowlisted |
| `MigrationDownMethods_ShouldNotDropDatabase` | Never permit database drop in any migration |
| `ProductionInitializer_ShouldNotAutoMigrateOutsideDevelopment` | Guard against startup migrations in Staging/Production |
| `PostgreSQLAndSqlServer_ModelSnapshots_ShouldDefineSameTables` | Table parity between providers |

### Destructive operation allowlist

If an `Up()` migration must drop data or schema (rare), add a justified entry to:

[`infra/azure/scripts/destructive-migration-allowlist.txt`](../../infra/azure/scripts/destructive-migration-allowlist.txt)

Format: `MigrationFileName.cs:reason`

---

## Applying migrations to Azure SQL

### Pipeline (recommended)

The [Deploy workflow](../../.github/workflows/deploy.yml) runs [`run-azure-sql-migrations.sh`](../../infra/azure/scripts/run-azure-sql-migrations.sh):

1. Runs `validate-migrations.sh`
2. Verifies the database exists (created by Bicep — **not** by EF)
3. Opens a temporary SQL firewall rule for the runner IP
4. Lists pending migrations (`--dry-run` step in CI)
5. Applies `dotnet ef database update`
6. Confirms no pending migrations remain
7. Removes the firewall rule

### Manual (operator)

```bash
az login
export CONTENTFORGE_MIGRATION_ENVIRONMENT=staging   # or production
export CONTENTFORGE_CONFIRM_PRODUCTION_MIGRATION=true   # required for production

./infra/azure/scripts/run-azure-sql-migrations.sh \
  rg-contentforge-staging \
  sql-cf-staging-xxxxx \
  contentforge
```

Dry-run only:

```bash
./infra/azure/scripts/run-azure-sql-migrations.sh \
  rg-contentforge-staging sql-cf-staging-xxxxx contentforge --dry-run
```

### Permissions

| Identity | Permission | When |
|----------|------------|------|
| API managed identity | `db_datareader`, `db_datawriter` | Runtime (post-deploy SQL script) |
| Pipeline / migration identity | Azure AD SQL admin or `db_ddladmin` | Migration job only |
| API managed identity | `db_ddladmin` | **Not** recommended for production |

See [`grant-api-sql-access.sql`](../../infra/azure/scripts/grant-api-sql-access.sql) and [`grant-api-sql-migration.sql`](../../infra/azure/scripts/grant-api-sql-migration.sql).

---

## What the pipeline never does

- `dotnet ef database drop`
- `EnsureDeleted()` / `DropDatabase()` in migrations
- Create a missing production database (must exist from Bicep)
- Recreate or replace an existing database
- Run migrations on API container startup in Staging/Production
- Skip migrations in production
- Deploy application containers when migration job failed

---

## Rollback limitations

EF Core **down migrations are not used** in production. Rollback is asymmetric:

### Application rollback (supported)

Redeploy a previous container image **only if** schema is backward-compatible with that build.

If a migration added non-nullable columns, renamed tables, or removed columns, rolling back the app without rolling back the database will cause runtime errors.

### Schema rollback (limited)

| Approach | When to use | Limitation |
|----------|-------------|------------|
| **Forward-fix migration** | Minor issues, additive schema | Requires new commit + deploy |
| **Azure SQL point-in-time restore (PITR)** | Bad migration applied, data corruption | Restores entire database to a timestamp; may lose legitimate writes since that time |
| **`dotnet ef database update <PreviousMigration>`** | Dev/staging only | Not supported as production runbook — can destroy data if migration is destructive |

### After PITR

1. Restore database to pre-migration timestamp (Azure Portal / CLI)
2. Redeploy last known-good **application** image compatible with restored schema
3. Verify `/health/ready`, login, and critical flows
4. Document incident; add forward migration if schema change is still required

### Coordination rule

**Never** roll back application containers to an older build while the database remains at a newer migration level unless engineers have verified compatibility.

See also [azure-cd.md — Rollback](./azure-cd.md#rollback).

---

## Authoring guidelines

1. **One logical change per migration** where practical
2. **Prefer additive changes** — new columns nullable or with defaults first
3. **Avoid destructive `Up()`** — use allowlist + explicit review if unavoidable
4. **Test locally** against PostgreSQL; run `validate-migrations.sh` before PR
5. **Document breaking changes** in the PR description
6. **Keep snapshots aligned** — Azure SQL and PostgreSQL must expose the same tables

---

## Troubleshooting

| Symptom | Cause | Action |
|---------|-------|--------|
| `has-pending-model-changes` fails | Model edited without migration | Add migration to both provider projects |
| Schema parity test fails | SqlServer snapshot out of date | Add/update SqlServer migration |
| `Database does not exist` | Bicep not deployed | Deploy infrastructure first |
| Migration auth failure | Pipeline identity not SQL admin | Add identity to SQL admin group |
| Destructive scan failure | Drop/delete in `Up()` | Refactor migration or allowlist with justification |
| App errors after deploy, migration OK | App/schema version mismatch | Check image tag vs applied migrations table `__EFMigrationsHistory` |

Query applied migrations on Azure SQL:

```sql
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;
```

---

## Related files

| File | Purpose |
|------|---------|
| [`validate-migrations.sh`](../../infra/azure/scripts/validate-migrations.sh) | Pre-deploy validation |
| [`run-azure-sql-migrations.sh`](../../infra/azure/scripts/run-azure-sql-migrations.sh) | Safe Azure SQL apply |
| [`destructive-migration-allowlist.txt`](../../infra/azure/scripts/destructive-migration-allowlist.txt) | Approved destructive `Up()` ops |
| [`MigrationSafetyTests.cs`](../../tests/ContentForge.ArchitectureTests/MigrationSafetyTests.cs) | Automated safety checks |
| [`MigrationSchemaParityTests.cs`](../../tests/ContentForge.ArchitectureTests/MigrationSchemaParityTests.cs) | PostgreSQL / Azure SQL table parity |
