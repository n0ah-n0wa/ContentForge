# ADR-007: Publishing Architecture (Draft vs Published)

**Status:** Accepted  
**Date:** 2026-08-29

## Context

Admin editing must not expose incomplete drafts on the public API. Draft and published representations can diverge while an entry is being revised.

## Decision

- Keep **draft data** in `DraftDataJson` (working copy)
- On publish, write a **published snapshot** to `PublishedSnapshotJson` (plus `PublishedAt` and status)
- Public endpoints (`/api/v1/public/...`) read **published snapshot only**
- Unpublish/archive clear or stop exposing published representation per domain rules
- Optimistic concurrency (`ConcurrencyToken`) protects concurrent editors

## Consequences

- Clear separation between editorial workspace and consumer-facing content
- Slightly more complex update/publish logic than a single JSON blob
- Search/list filters must distinguish status and published presence correctly
