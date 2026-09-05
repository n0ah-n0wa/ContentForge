# ADR-001: Clean Architecture

**Status:** Accepted  
**Date:** 2026-08-29

## Context

ContentForge must demonstrate production-grade .NET engineering with clear separation of concerns (SPEC §4, §118).

## Decision

Adopt Clean Architecture with backend projects:

- `ContentForge.Domain` — entities, value objects, domain rules
- `ContentForge.Application` — use cases, DTOs, validators, ports
- `ContentForge.Infrastructure` — EF Core (PostgreSQL), Identity, storage, external services
- `ContentForge.Infrastructure.SqlServer` — Azure SQL EF migrations for cloud
- `ContentForge.Api` — HTTP, middleware, composition root

Dependency direction flows inward: Api → Application → Domain ← Infrastructure.

Architecture tests enforce forbidden dependency directions.

## Consequences

- Domain logic remains framework-agnostic and unit-testable
- Infrastructure can be swapped (PostgreSQL/Azure SQL, local/Azure Blob) without domain changes
- Slightly more project overhead than a monolith, justified by portfolio and maintainability goals
