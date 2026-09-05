# ADR-003: Dynamic Content Schema Representation

**Status:** Accepted  
**Date:** 2026-08-29

## Context

ContentForge must support administrator-defined content types with typed fields without generating new CLR entities or tables per type (SPEC content-type requirements).

## Decision

- Persist **schema** relationally (`ContentTypes`, `ContentTypeFields`) with a `FieldType` enum and JSON-friendly `FieldConfiguration`
- Persist **entry field values** as JSON on the content entry (`DraftDataJson`) validated in Domain/Application against the schema
- Support field add/update/rename/remove with explicit confirmation flags for destructive changes
- Supported field types: Text, LongText, RichText, Integer, Decimal, Boolean, Date, DateTime, Media, MediaMultiple, Relation, RelationMultiple, Select, MultiSelect, Json

## Consequences

- Flexible CMS modeling without schema migrations per type
- Validation complexity lives in Domain (`ContentDataValidator`) rather than DB constraints
- JSON querying/search is limited compared to fully relational columns; mitigated by metadata columns and portable search
