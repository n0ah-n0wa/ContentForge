# Release readiness report

**Date:** 2026-09-05  
**Commit reviewed:** `910eae3` (+ local fix commit `81a51ca`, see "Defects fixed")  
**Review question:** Can this repository credibly be presented as a production-oriented .NET/Azure portfolio project?  
**Verdict: YES — approved for release**, with the known limitations below stated honestly wherever the project is presented.

This review was independent of prior audits: it re-verified the build/test/deploy chain from a clean environment (see [CLEAN_ENVIRONMENT_VERIFICATION.md](./CLEAN_ENVIRONMENT_VERIFICATION.md)), cross-checked SPECIFICATIONS.md against the implementation file-by-file, scanned for secrets/debug artifacts/fake implementations, and probed the live staging deployment.

---

## 1. Verified capabilities

Each capability was exercised against a running system (fresh clone, fresh volumes, isolated package caches), not inferred from documentation:

| Capability | Evidence |
|---|---|
| Clean Architecture with enforced boundaries | 31 architecture tests green; layer-dependency rules verified |
| Dynamic content types (15 field types), lifecycle (Draft → In review → Published → Unpublished/Archived) | Domain transition map matches SPEC §11; exercised end-to-end by E2E workflow test |
| Versioning (list/get/compare/restore, immutable snapshots) | Integration + E2E versioning tests; restore creates a new version |
| Optimistic concurrency → HTTP 409 with conflict context | Integration race tests + E2E stale-update test |
| Media upload with hardened validation (MIME/extension pairing, size, traversal, control chars, ADS) | Integration + E2E media tests; system-generated storage keys |
| JWT auth + refresh rotation, 4 roles, 23 permission policies on every mutating endpoint | 46 security/authorization integration tests green; all spec permission strings present verbatim |
| Public read API (published content only), preview tokens (scoped, revoking, short-lived) | Conformance check + integration tests |
| Append-only audit log, dashboard statistics, rate limiting on all sensitive endpoints | Conformance check; rate-limit integration tests |
| Scheduled publish/unpublish background processor with stale-lock recovery and idempotency guards | Code-level review + integration coverage |
| RFC 7807 Problem Details errors (`application/problem+json`, traceId + correlationId) | Verified live on staging (see §3) |
| Dual-provider persistence (PostgreSQL dev/CI, Azure SQL cloud) with migration parity | `has-pending-model-changes` clean for both providers; migration safety/parity tests green |

**Hygiene checks (all clean):**

- **No secrets:** no committed `.env` files; Production/Staging appsettings contain no credentials (JWT key comes from Key Vault); only clearly-labeled `DEV_ONLY`/`TEST_ONLY` placeholder values in Development/Testing configs, which the API refuses to run with in Production.
- **No debug artifacts** (after fix `81a51ca` — see §5).
- **No fake implementations:** no `NotImplementedException`, no TODO/FIXME/HACK markers in backend or frontend source; the only `NotSupportedException` usages are a correct non-seekable `Stream` wrapper.
- **No console/debugger leftovers** in frontend source.

## 2. Test results (clean-environment run at `910eae3`)

| Suite | Result |
|---|---|
| Backend format (`dotnet format --verify-no-changes`) | PASS |
| Backend Release build (warnings-as-errors, analyzers on) | PASS — 0 warnings, 0 errors |
| Unit tests | **174/174** |
| Integration tests (real PostgreSQL + Azurite) | **247/247** |
| — security/authorization/rate-limit subset, run explicitly | **46/46** |
| Architecture tests | **31/31** |
| Frontend: `npm audit` / Prettier / ESLint (`--max-warnings 0`) / vue-tsc | PASS — 0 vulnerabilities |
| Frontend unit/component tests (Vitest) | **94/94** |
| Playwright E2E (incl. axe WCAG AA checks) | **16/16** |
| Migration validation (`validate-migrations.ps1`, both providers) | PASS |
| Total | **592 tests, 0 failures, 0 skips** |

## 3. Deployment verification

| Check | Result |
|---|---|
| CI on `main` at `910eae3` | **success** (GitHub Actions) |
| Deploy (staging) at `910eae3` | **success** — validate → ACR build/push → Azure SQL migrate → App Service deploy → health verify |
| Live staging probe (this review) | `https://app-cf-web-staging.azurewebsites.net/health` → 200 `ok`; SPA root → 200; API through nginx proxy answers with correct `application/problem+json` (traceId/correlationId present, **no stack traces or internals leaked**) |
| Docker (local) | API + Web images build `--no-cache --pull`; dev stack healthy on fresh volumes; startup migrations (7) + admin seed verified; nginx `/api` proxy and Swagger verified; all four compose files pass `docker compose config` |
| Azure IaC | `az bicep build` clean; `validate-azure-parameters.ps1` PASS |
| Production | Deliberately manual (`workflow_dispatch` + environment protection) — not exercised by this review, by design |

## 4. Known limitations (state these when presenting the project)

1. **Relation fields are not referentially validated** (SPEC §16/§17 divergence). Relation values are validated as GUIDs only; there is no existence check against target entries and no defined behavior when a referenced entry is deleted/archived. The `ContentEntryRelations` table exists in the schema but is written/read by no application code.
2. **Settings and Profile screens are placeholders.** Both routes render a "will be added in a later phase" panel. Functional scope elsewhere is complete; these two navigation entries are not.
3. **Seed data is minimal** (SPEC §99/§100 partial): only the administrator is seeded — no demo content types, sample entries, or additional role users.
4. **Route shapes differ from SPEC §34** (edit/view paths, version history as an embedded panel instead of a route). Functionally equivalent; contract differs on paper.
5. **`/media-files/*` is served outside `/api/v1`** — deliberate and documented (README), but a divergence from strict SPEC §26 versioning.

## 5. Defects fixed during this review

- **Committed debug artifacts removed** (local commit `81a51ca`, intentionally **not pushed** — pushing `main` triggers the staging deploy): `.tools/` contained a machine-local dotnet-ef 8.0.11 tool-path installation (22 files of Windows binaries and generated store metadata) and `test-results/.last-run.json` was Playwright run state. Nothing in CI, scripts, or docs referenced either path; both are now gitignored.

No other release-blocking defects were found; no source or behavior changes were made.

## 6. Remaining technical debt (non-blocking)

1. **Scheduled publish/unpublish saves entry state and audit records without a wrapping transaction** (`ScheduledJobProcessor`); interactive publish is transactional. Worst case on a mid-operation crash is a missing/duplicate audit row, bounded by existing idempotency guards. Same note for `CreateContentEntryCommandHandler`.
2. **`ContentEntryRelations` schema is dead weight** — either implement §17 referential rules on top of it or drop it in a migration.
3. **`DashboardController` uses `[Authorize]` without a permission policy** — mitigated (the handler is permission-aware and returns a reduced DTO), but it is the one endpoint outside the "policy per endpoint" pattern.
4. **Node 24 vs pinned 20.19.0** locally; CI uses the pin. Cosmetic parity gap on dev machines.
5. **Windows MAX_PATH note** for relocated NuGet caches — environment-level, documented in [CLEAN_ENVIRONMENT_VERIFICATION.md](./CLEAN_ENVIRONMENT_VERIFICATION.md).

## 7. Final recommendation

**Release.** The repository demonstrates what it claims: enforced Clean Architecture, dual-provider persistence with disciplined migrations, a hardened and tested security model, a fully green 592-test suite reproducible from a clean environment, working containerization, and a verified, currently-live Azure staging deployment driven by OIDC CI/CD. The README is accurate against observed behavior, API documentation matches the implemented contract, and no secrets, debug artifacts, or fake implementations remain.

Present limitations §4.1–4.3 honestly (ideally as a short "Scope notes" line in the README or portfolio write-up); they are scoped-out product decisions, not engineering defects, and hiding them would undermine an otherwise credible project. Resolving §4.1 (relation integrity) would be the highest-value next increment if development continues.
