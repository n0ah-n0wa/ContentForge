# Operations Documentation

Runbooks and deployment guides for ContentForge. All procedures below match scripts and workflows that exist in this repository or platform features configured by Bicep.

## Index

| Document | Topics |
|----------|--------|
| [ci.md](./ci.md) | GitHub Actions CI gates |
| [azure-cd.md](./azure-cd.md) | Continuous deployment, OIDC, **rollback** |
| [azure-deployment.md](./azure-deployment.md) | Azure deploy how-to |
| [azure-infrastructure.md](./azure-infrastructure.md) | Resource topology |
| [azure-security.md](./azure-security.md) | Security model, backup retention matrix |
| [azure-storage.md](./azure-storage.md) | Blob media operations |
| [azure-observability.md](./azure-observability.md) | Application Insights / monitoring |
| [database-migrations.md](./database-migrations.md) | EF migrations (PostgreSQL + Azure SQL) |
| [docker.md](./docker.md) | Container deployment |
| [backups-and-recovery.md](./backups-and-recovery.md) | Backups, PITR, media/Key Vault recovery |
| [PRODUCTION_READINESS_REPORT.md](./PRODUCTION_READINESS_REPORT.md) | Independent production-readiness review (**does not certify readiness**) |
| [SPECIFICATION_COMPLIANCE.md](./SPECIFICATION_COMPLIANCE.md) | Line-by-line SPEC compliance checklist (IMPLEMENTED / PARTIAL / NOT) |

## Quick links

- Infra as code: [`infra/azure/README.md`](../../infra/azure/README.md)
- Docker images: [`infra/docker/README.md`](../../infra/docker/README.md)
- Architecture: [`../ARCHITECTURE.md`](../ARCHITECTURE.md)
- ADRs: [`../decisions/`](../decisions/)

## Operational principles

1. **No secrets in git** — use Key Vault / GitHub OIDC / local env files that are gitignored.
2. **Migrations before app** — cloud schema changes run in the migrate job; API does not migrate on startup in Staging/Production.
3. **Staging before production** — production deploy is manual with environment protection.
4. **Rollback prefers previous images** — schema rollback is a separate, careful decision (forward-fix preferred).
5. **Documented ≠ automated** — backups are Azure platform features; restore drills are an operator responsibility.
