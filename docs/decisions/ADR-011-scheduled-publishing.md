# ADR-011: Scheduled Publishing Mechanism

**Status:** Accepted  
**Date:** 2026-08-31

## Context

Editors need publish/unpublish at a future time without an external job scheduler product.

## Decision

- Store `ScheduledPublishAt` / `ScheduledUnpublishAt` on content entries
- Expose schedule APIs: `PUT/DELETE /api/v1/content/{id}/schedule`
- Run an in-process **`ScheduledPublishingBackgroundService`** that polls due work using configurable `ScheduledPublishing` options (interval, batch size, lock duration, retries)
- Claim jobs with concurrency-safe updates (provider-specific SQL where required) and process publish/unpublish through domain methods
- Keep scheduling disabled or irrelevant when the host is not running (single-instance assumption for v1)

## Consequences

- No dependency on Hangfire/Azure WebJobs for v1
- Multi-instance scale-out requires stronger distributed locking than the current design assumes
- Operators tune poll interval/batch via configuration
