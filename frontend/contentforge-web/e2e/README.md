# ContentForge E2E (Playwright)

Browser end-to-end tests for critical admin journeys (SPEC §64).

## Stable environment

Use the isolated Compose stack (separate project name, ports, and volumes from local-dev):

```bash
# repository root
docker compose -f docker-compose.e2e.yml up -d --build

# wait until web is healthy, then:
cd frontend/contentforge-web
npm ci
npm run e2e:install
npm run test:e2e

# flake check (3× each test)
npm run test:e2e:repeat

# tear down and wipe E2E data
docker compose -f docker-compose.e2e.yml down -v
```

| Service  | URL                                  |
| -------- | ------------------------------------ |
| Admin UI | http://localhost:28080               |
| API      | http://localhost:25080               |
| Postgres | localhost:25432 (`contentforge_e2e`) |

Seeded admin (Development initializer): `admin@contentforge.local` / `AdminPassword123!`

Override endpoints with `E2E_BASE_URL` and `E2E_API_BASE_URL` when needed.

**Important:** Rebuild the `web` image after frontend changes that affect E2E (`data-testid`, forms, media picker). Compose `--build` does this in CI.

## Critical scenarios

| Spec                       | Coverage                                                                                                      |
| -------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `auth.spec.ts`             | Login, invalid login, logout (+ session cleared), protected route, forbidden access, disabled account         |
| `content-workflow.spec.ts` | Create, edit, save draft, submit, publish, public API visibility, unpublish, public API invisibility, archive |
| `versioning.spec.ts`       | Multiple versions, inspect history, restore previous version                                                  |
| `authorization.spec.ts`    | Author cannot publish; Viewer cannot open edit route; Editor can publish; Administrator manages users         |
| `media.spec.ts`            | Upload, select on content entry, delete (+ API 404)                                                           |
| `concurrency.spec.ts`      | Stale concurrency token conflict + reload                                                                     |
| `accessibility.spec.ts`    | axe-core WCAG 2.1 A/AA smoke on login + authenticated dashboard shell                                         |

## Layout

| Path            | Purpose                                                                       |
| --------------- | ----------------------------------------------------------------------------- |
| `e2e/specs/`    | Journey specs                                                                 |
| `e2e/pages/`    | Page objects                                                                  |
| `e2e/support/`  | API client, auth/data helpers, public API helpers, cleanup registry, fixtures |
| `e2e/fixtures/` | Upload assets                                                                 |

Tests drive the UI for user workflows. API helpers set up/tear down isolated data and assert public API visibility / media deletion.

## Determinism & synchronization

- Unique emails/slugs per test (`uniqueSuffix` with timestamp + crypto bytes)
- Cleanup registry deletes content, media, content types, and disables users after each test
- Isolated Compose database (never shared with unit/integration CI Postgres)
- Chromium-only project, **single worker** (avoids cross-test login contention)
- No arbitrary `waitForTimeout` sleeps — waits are on URL, role/dialog visibility, status text, validation state, or polled public API status
- Lifecycle actions optionally wait for the resulting entry status
- Public API visibility uses `expect.poll` to tolerate cache invalidation lag
- Post-create URL waits exclude `/new` routes; dialogs fill change summaries when required

## Known limitations

1. **Shared seeded administrator** — All tests that need admin rights authenticate as `admin@contentforge.local`. Tests must not change that account’s password or disable it. Per-test users/content are otherwise isolated.
2. **Single worker** — `workers: 1` keeps auth and DB load predictable. The suite is not validated under parallel Playwright workers.
3. **UI login every time** — Tests intentionally sign in through the login form (not `storageState`) so auth regressions are caught. This increases suite duration.
4. **Chromium only** — Firefox/WebKit are out of scope for CI cost and selector differences.
5. **Best-effort cleanup** — Failed deletes (already-deleted rows, FK races) are swallowed so one cleanup failure does not mask the original assertion. Leftover rows are wiped by `docker compose … down -v`.
6. **E2E test IDs** — Stable hooks use `data-testid` on status, validation, concurrency panel, version history, and media selection. Prefer role/label queries elsewhere; treat test ids as part of the E2E contract when changing markup.
7. **Form-field CSS fallback** — Some CMS fields still lack standalone accessible names beyond wrapping `<label class="form-field">`; page objects locate those via labeled form-field containers until the UI adds explicit `aria-label`s.
8. **Media picker click-through** — Confirming a selection defers overlay close and disables the trigger while open (product fix). E2E relies on that behavior.
9. **Rate limiting** — `docker-compose.e2e.yml` raises auth/public/media permit limits. Running E2E against a non-e2e API with default limits will flake on login.
10. **Public content cache** — Visibility assertions poll for up to 15s; a stuck cache would fail the suite rather than pass silently.
11. **Media soft delete** — Deleted assets disappear from the library list and are marked `isDeleted`, but admin GET-by-id still returns 200.
