# ADR-009: CI/CD and Migration Deployment

**Status:** Accepted  
**Date:** 2026-08-31

## Context

Schema changes must be safe across PostgreSQL (dev/CI) and Azure SQL (cloud). Automatic migrate-on-startup is unsafe for shared staging/production databases.

## Decision

- Commit EF migrations in **two projects**: `ContentForge.Infrastructure` (PostgreSQL) and `ContentForge.Infrastructure.SqlServer` (Azure SQL)
- CI validates migrations via `infra/azure/scripts/validate-migrations.sh` (pending model changes, destructive Up() allowlist)
- Development may apply PostgreSQL migrations on API startup
- Staging/Production apply Azure SQL migrations only in the deploy pipeline (`run-azure-sql-migrations.sh`) **before** App Service image update
- GitHub Actions: `ci.yml` for PRs/pushes; `deploy.yml` for OIDC deploy to staging/production

## Consequences

- Dual migration sets must stay in sync when the model changes
- Failed migrate jobs block container deploy
- Operators need DDL grants for the migration identity, separate from runtime `db_datareader`/`db_datawriter`
