# Azure Blob Storage — Media Operations

**Status:** Operational guide  
**Source of truth:** [SPECIFICATIONS.md](../../SPECIFICATIONS.md) §18–19, §75–76

ContentForge stores media **metadata** in the relational database and **binaries** in object storage. Production uses **Azure Blob Storage** through the Application port `IFileStorage`; Azure SDK types remain in Infrastructure only.

---

## Architecture

```text
Application (IFileStorage)
        │
        ▼
Infrastructure
  AzureBlobFileStorage  ──►  IBlobStorageGateway
                                    │
                                    ▼
                          AzureBlobStorageGateway
                                    │
                                    ▼
                          Azure.Storage.Blobs SDK
```

| Provider (`Media:Provider`) | Use case | Credentials |
|----------------------------|----------|-------------|
| `Local` (default) | Developer machines, no Azure | None — filesystem under `Media:LocalRoot` |
| `InMemory` | Isolated unit tests | None |
| `Azure` | Production, staging, optional Azurite | Managed identity **or** connection string |

Local development **does not require Azure credentials** when `Media:Provider` is `Local`.

---

## Required Azure resources

1. **Storage account** (General Purpose v2 recommended)
2. **Blob container** (default name: `media`)
3. **Managed identity** on the hosting app (App Service, Container Apps, AKS workload identity, etc.)
4. **Role assignment:** `Storage Blob Data Contributor` on the container or storage account for the app identity
5. **(Optional) CDN / Front Door** in front of the blob endpoint for public delivery

Do **not** store account keys in source control, Docker images, or workflow files.

---

## Configuration reference

All settings live under the `Media` configuration section.

| Key | Required | Description |
|-----|----------|-------------|
| `Provider` | Yes | `Azure` for blob storage |
| `ContainerName` | Yes | Blob container name (e.g. `media`) |
| `PublicBaseUrl` | Yes | URL prefix stored in media metadata (CDN or blob path prefix) |
| `UseManagedIdentity` | Production | `true` when using managed identity |
| `StorageAccountName` | Production* | Account name; used to build `https://{name}.blob.core.windows.net` |
| `BlobServiceUri` | Alternative | Full service URI if not using default public endpoint |
| `ConnectionString` | Dev/Azurite only | **Secret** — supply via environment or Key Vault reference |

\* Provide either `StorageAccountName` or `BlobServiceUri` when using managed identity.

### Production (managed identity)

Set via App Service configuration, Key Vault references, or environment variables:

```bash
Media__Provider=Azure
Media__ContainerName=media
Media__UseManagedIdentity=true
Media__StorageAccountName=contentforgeprod
Media__PublicBaseUrl=https://cdn.example.com/media
```

The application uses `DefaultAzureCredential`, which resolves to the App Service managed identity in Azure.

### Staging

Mirror production: managed identity, separate storage account/container, staging CDN prefix.

---

## Local development without Azure

Default `appsettings.Development.json` uses:

```json
"Media": {
  "Provider": "Local",
  "LocalRoot": "App_Data/media",
  "PublicBaseUrl": "/media-files"
}
```

No Azure subscription or credentials are needed.

---

## Optional: Azurite emulator

Docker Compose includes Azurite on port `10000`:

```bash
docker compose up -d azurite
```

To exercise the Azure adapter locally, override configuration (User Secrets or environment — **do not commit**):

```bash
Media__Provider=Azure
Media__ContainerName=media
Media__ConnectionString=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
Media__PublicBaseUrl=http://127.0.0.1:10000/devstoreaccount1/media
```

The Azurite account key is a **public development constant** documented by Microsoft; it must never be used for real storage accounts.

Integration test `AzuriteBlobStorageIntegrationTests` probes `localhost:10000` and no-ops when Azurite is unavailable.

---

## Security checklist

- [ ] Production uses `UseManagedIdentity=true` and **no** connection string
- [ ] App identity has least-privilege blob data role (Contributor on container scope)
- [ ] Container public access is **disabled** (`PublicAccessType.None`); serve via CDN or authenticated API as designed
- [ ] Secrets supplied through Key Vault references or platform secret stores
- [ ] `PublicBaseUrl` points to controlled delivery endpoint (CDN), not raw storage keys
- [ ] Upload validation (MIME, extension, size, path-safe keys) remains enforced in Application/Domain

---

## Operations

### Container bootstrap

On startup, `AzureBlobStorageInitializer` calls `CreateIfNotExists` for the configured container when `Provider=Azure`. Infrastructure-as-code may also provision the container; duplicate creation is harmless.

### Backup and retention

Configure storage redundancy (LRS/GRS) and lifecycle management in Azure per organizational policy. Media metadata in SQL should be backed up with the main database.

### Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| `403 AuthorizationFailure` on upload | Managed identity missing `Storage Blob Data Contributor` |
| `InvalidOperationException` at startup | Missing `ContainerName` or neither connection string nor managed identity settings |
| Works locally, fails in Azure | `UseManagedIdentity` false, or wrong account name |
| Azurite tests skipped | Emulator not running — start with `docker compose up -d azurite` |

---

## Related documents

- [ARCHITECTURE.md](../ARCHITECTURE.md) — media storage abstraction
- [IMPLEMENTATION_PLAN.md](../IMPLEMENTATION_PLAN.md) — Phase 6 / Phase 16
- [DEVELOPMENT_RULES.md](../DEVELOPMENT_RULES.md) — secrets handling
