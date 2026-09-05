# ADR-005: Media Storage Abstraction

**Status:** Accepted  
**Date:** 2026-08-29

## Context

Media binaries must work locally (disk / Azurite) and in Azure Blob Storage with managed identity, without Domain coupling to Azure SDKs.

## Decision

- Define `IFileStorage` in Application/Domain ports
- Configure provider via `Media:Provider` = `Local` or `Azure`
- Local: filesystem under `Media:LocalRoot` with public URL base for serving
- Azure: Blob container (Azurite in Compose; Azure Storage in cloud) with connection string or managed identity (`UseManagedIdentity`)
- Serve bytes through `GET /media-files/{**storageKey}` after authorization/soft-delete checks
- Validate uploads (extension, content-type, size, content inspection) before storage

## Consequences

- Same application code path for all environments
- Soft-deleted media can be gated at serve time
- Operators must keep `Media` config aligned with the active store (Local vs Azure)
