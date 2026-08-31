# Azure Infrastructure

ContentForge Azure resources are defined as Bicep templates under [`infra/azure/`](../../infra/azure/README.md).

## Quick reference

| Environment | Resource group | Parameter file |
|-------------|----------------|----------------|
| Development | `rg-contentforge-dev` | `infra/azure/bicep/parameters/dev.bicepparam` |
| Staging | `rg-contentforge-staging` | `infra/azure/bicep/parameters/staging.bicepparam` |
| Production | `rg-contentforge-prod` | `infra/azure/bicep/parameters/prod.bicepparam` |

## Before first deploy

1. Review and customize parameter files (Azure AD admin, container images, region, tags).
2. Build and push API/web container images (see [Docker deployment](./docker.md)).
3. Validate templates with `az deployment group validate` — **do not deploy until approved**.

Full prerequisites, provisioning commands, and post-deploy steps (Key Vault secrets, SQL MI grant, migrations) are documented in **[infra/azure/README.md](../../infra/azure/README.md)**.

## Resources provisioned

- Linux App Service Plan with API and Web container apps
- Azure SQL Database (Azure AD admin, managed identity connection from API)
- Blob Storage for media (managed identity, no shared keys)
- Log Analytics + Application Insights
- Key Vault for JWT signing key (Key Vault reference in App Service settings)

## Related

- [Azure deployment guide](./azure-deployment.md)
- [Azure security model](./azure-security.md)
- [Azure Blob Storage (media)](./azure-storage.md)
- [Azure Observability (Application Insights)](./azure-observability.md)
