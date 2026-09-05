# ADR-008: Search Abstraction

**Status:** Accepted  
**Date:** 2026-08-29

## Context

Admin users need filter, sort, pagination, and keyword search across content. PostgreSQL and Azure SQL differ in full-text capabilities.

## Decision

- Introduce `IContentSearchService` in Application
- Implement `EfContentSearchService` with **portable** keyword matching (`PortableSearch` / EF expressions) plus relational filters (type, status, author, dates) and sort
- Do **not** require Elasticsearch or Azure Cognitive Search for v1
- Keep the port so a dedicated search engine can be swapped later without Application rewrites

## Consequences

- Predictable behavior on both database providers
- Keyword search is substring/contains-oriented, not ranked full-text relevance
- Adequate for portfolio/CMS admin scale; upgrade path exists via the interface
