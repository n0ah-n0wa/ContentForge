# Backups and Recovery

ContentForge relies on **platform-managed Azure backups** and documented restore procedures. The repository does **not** include custom backup/restore scripts.

**Related:** [azure-security.md](./azure-security.md) · [azure-cd.md](./azure-cd.md) · [database-migrations.md](./database-migrations.md) · [azure-storage.md](./azure-storage.md)

---

## What is backed up

| Component | Mechanism | Where configured |
|-----------|-----------|------------------|
| **Azure SQL Database** | Point-in-time restore (PITR); long-term retention (LTR) in **prod** | Bicep SQL module / parameters (`infra/azure/bicep/`) |
| **Blob media** | Soft delete + versioning (staging/prod) | Bicep storage module |
| **Key Vault secrets** | Soft delete; purge protection outside dev | Bicep Key Vault module |
| **Container images** | Immutable tags in ACR (`:sha`, `:{env}-latest`) | Deploy workflow |
| **Local Docker volumes** | Operator-managed (`postgres_data`, `api_media`, `azurite_data`) | `docker compose` — **not** automated cloud backup |

### Retention (defaults from ops docs / parameters)

| Environment | SQL PITR | SQL LTR | Blob soft delete |
|-------------|----------|---------|------------------|
| dev | ~7 days | Off | Typically off |
| staging | ~7 days | Off | On |
| prod | **35 days** | Weekly/monthly/yearly | On |

Confirm live settings in the Azure portal or Bicep parameters for your subscription — treat the table as the intended matrix, not a live query.

---

## Recovery procedures

### 1. Application rollback (bad release, good data)

Redeploy the **previous container image tags** for API and Web. The deploy workflow records prior tags for rollback guidance — see [azure-cd.md](./azure-cd.md).

There is **no automatic** rollback job on failed verify; operators re-run deploy with the known-good SHA or use Azure portal / CLI to set previous images.

Do **not** roll back schema blindly: if a migration already applied, prefer a forward-fix migration (see [database-migrations.md](./database-migrations.md)).

### 2. Azure SQL point-in-time restore

Use when data corruption or accidental deletes occur within the PITR window:

1. In Azure Portal (or CLI), restore the database to a **new** database name at the chosen timestamp.
2. Validate data on the restored copy.
3. Swap connection / rename databases per your change window, or point App Service `Database__ConnectionString` at the restored database.
4. Re-grant managed identity users if restoring to a new database name (`infra/azure/scripts/grant-api-sql-access.sql`).
5. Restart API App Service and verify `/health/ready`.

LTR restores (prod) follow Azure SQL long-term backup restore docs when PITR window is insufficient.

### 3. Blob / media recovery

1. Soft-deleted blobs: undelete via Azure Storage tooling within the soft-delete retention period.
2. Versioned blobs: restore a prior version of the object.
3. If media metadata in SQL and blobs diverge, repair metadata carefully or re-upload — there is no automated reconcile tool in-repo.

### 4. Key Vault secret recovery

1. Recover a soft-deleted secret version if deleted accidentally.
2. Update App Service Key Vault reference if the secret URI/version changed.
3. Restart API to pick up configuration.

### 5. Local development reset

```bash
docker compose down -v
docker compose up -d --build
```

This **destroys** local Postgres/media/Azurite volumes. It is not a production recovery path.

---

## Verification after recovery

- [ ] `GET /health/live` and `GET /health/ready` succeed (via Web proxy and/or Cloud Shell for API)
- [ ] Admin login works
- [ ] Spot-check a published public entry and a media URL
- [ ] Confirm App Insights / logs receiving traffic
- [ ] Confirm JWT Key Vault reference resolves (API starts without config errors)

---

## Responsibilities

| Role | Duty |
|------|------|
| Platform | Azure SQL automated backups, blob soft-delete, Key Vault soft-delete |
| Operators | Test restore periodically before go-live; document RPO/RTO targets for the org |
| Developers | Never delete production databases from app code; never run `database drop` in pipelines |

SPECIFICATIONS.md §79 expects restore procedures to be documented and tested — this runbook is that documentation. Execute the drill below before relying on production.

---

## Backup configuration verification (automated)

When Azure CLI access is available:

```bash
RESOURCE_GROUP=rg-contentforge-staging \
  ./infra/azure/scripts/verify-backup-configuration.sh

RESOURCE_GROUP=rg-contentforge-prod ENVIRONMENT=prod \
  ./infra/azure/scripts/verify-backup-configuration.sh
```

This checks SQL short-term retention (and LTR presence in prod) plus blob soft-delete/versioning flags.

---

## Disaster-recovery drill checklist

Record evidence (date, operator, resource group, timestamps, screenshots/CLI output) in your ops ticket — do not commit secrets.

1. **Config verify** — run `verify-backup-configuration.sh` for the environment.  
2. **SQL PITR dry-run** — in Portal/CLI, create a restore to a **new** database name at T-15 minutes; do not cut over yet.  
3. **Validate restored copy** — confirm schema + a known content row.  
4. **Delete the restore copy** after validation (cost control).  
5. **Blob soft-delete** — upload a test blob, soft-delete, undelete within retention.  
6. **App rollback rehearsal** — identify previous ACR image tags from the last deploy job summary (no production traffic change required for a tabletop).  
7. **Sign off** — store evidence with the change ticket.

Until steps 1–6 have been executed at least once against a real subscription, treat recovery as **unproven**.
