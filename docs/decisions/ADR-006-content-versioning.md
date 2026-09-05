# ADR-006: Content Versioning Strategy

**Status:** Accepted  
**Date:** 2026-08-29

## Context

Editors need history, compare, and restore without losing published state accidentally.

## Decision

- Maintain an ordered list of **immutable `ContentVersion` snapshots** per entry
- Advance `CurrentVersion` on meaningful edits and lifecycle actions that record history
- Expose list/get/compare/restore APIs under `/api/v1/content/{id}/versions...`
- Restore creates a new version from a historical snapshot (does not rewrite history)
- Require change summaries where lifecycle/version APIs demand them

## Consequences

- Auditable editorial history suitable for CMS workflows
- Storage grows with edit volume; pruning is out of scope for v1
- Restore still subject to concurrency tokens and permissions (`content.version.restore`)
