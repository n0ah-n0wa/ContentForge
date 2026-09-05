# ContentForge — Final Frontend Production Review

**Date:** 2026-09-05  
**Scope:** Vue 3 admin SPA (`frontend/contentforge-web`) — components, composables, Pinia, API, routing, TypeScript, authz UI, a11y, security, tests  
**Authority:** [SPECIFICATIONS.md](../../SPECIFICATIONS.md), [ARCHITECTURE.md](../ARCHITECTURE.md), package `docs/accessibility.md`  
**Verification:** format · lint · typecheck · Vitest · Vite production build · Playwright E2E

---

## Executive summary

The ContentForge admin frontend is a **mature Vue 3 + Pinia + Vue Router SPA** with a typed `fetch` API client, sessionStorage JWT auth, permission-aware navigation, Problem Details error toasts, and solid unit/E2E/a11y coverage.

This pass found **no CRITICAL** defects. **HIGH** and several **MEDIUM** issues were fixed with regression tests. Remaining items are incremental polish, not release blockers.

| Severity | Found | Fixed this pass | Deferred |
|----------|------:|----------------:|---------:|
| CRITICAL | 0 | 0 | 0 |
| HIGH | 2 | 2 | 0 |
| MEDIUM | 7 | 5 | 2 |
| LOW | 6 | 2 | 4 |

**Quality gate:**

| Gate | Result |
|------|--------|
| `npm run format:check` | Pass |
| `npm run lint` (`--max-warnings 0`) | Pass |
| `npm run typecheck` | Pass |
| `npm run test` (Vitest) | **94 passed** |
| `npm run build:vite` | Pass |
| `npm run test:e2e` (Playwright / Compose) | **16 passed** |

---

## Architecture assessment

### Strengths

- Clear layering: `api/` · `stores/` (3 stores) · `composables/` · `views/` · `components/`
- Domain state kept in views/composables (not inflated global Pinia)
- `apiRequest` centralizes auth headers, 401 refresh retry, and loading overlay
- Route guards: `authGuard` + `permissionGuard` with `meta.permissions`
- Accessibility foundations: skip links, modal focus trap, labelled forms, axe unit + E2E
- Strict TypeScript (`strict`, unused locals/params); no `as any` / `@ts-ignore` in `src/`
- Tokens in **sessionStorage** (better than localStorage); backend remains the authz boundary

### Watch points (accepted / deferred)

| Item | Notes |
|------|-------|
| Custom rich-text sanitizer (not DOMPurify) | Hardened this pass; still prefer a maintained library long-term |
| `contenteditable` + `document.execCommand` | Known a11y/platform limits (documented) |
| Five permission composables | Thin facades; consolidate when the matrix grows |
| CSS-class E2E locators for form fields | Prefer roles/`getByLabel` over time |
| Stale `docs/ARCHITECTURE.md` §5 | Still mentions Axios / fictional Pinia domain stores |

---

## Findings fixed this pass

### HIGH-1 — Open redirect after login

- **Impact:** `LoginView` passed `route.query.redirect` to `router.replace` without validation → absolute/`//` URLs could navigate off-app.
- **Fix:** `resolveSafeInternalPath()` allowlists same-origin relative paths only.
- **Tests:** `navigationSafety.test.ts`, `loginView` open-redirect case.

### HIGH-2 — In-memory caches survived logout

- **Impact:** Media/relation caches retained entries (including failed promises) across sessions in the same SPA instance.
- **Fix:** Evict failed cache entries; clear both caches in `authStore.clearSession()`.

### MEDIUM — Notification timer leak

- **Impact:** `watch({ immediate })` + `onMounted` double-scheduled dismiss timers.
- **Fix:** Single watch path; clear/replace timers safely on change/unmount.

### MEDIUM — Rich-text XSS residual risk

- **Impact:** Protocol-relative `//…` hrefs allowed; `contenteditable` sync wrote unsanitized HTML.
- **Fix:** Block protocol-relative URLs; sanitize on editor sync.

### MEDIUM — Media upload lacked 401 refresh retry

- **Impact:** XHR upload path did not mirror `apiRequest` refresh behavior.
- **Fix:** One refresh retry via shared `getRefreshHandler()`.

### MEDIUM — Edit routes under-gated

- **Impact:** Entry/type/user **edit** routes only required `.read`, so viewers could open edit URLs (controls disabled, but route exposed).
- **Fix:** Require `content.update` / `contentType.update` / `user.update`. Viewers hit Access denied.
- **Tests:** Router guard unit test; E2E authorization spec updated.

### LOW — Media picker a11y + timer cleanup

- Dialog uses `aria-labelledby`; close `setTimeout` cleared on unmount.

---

## Remaining findings (deferred)

| ID | Severity | Finding | Rationale |
|----|----------|---------|-----------|
| D1 | MEDIUM | List views lack AbortController / generation guards | Fast filter/nav races rare; add when UX shows flicker |
| D2 | MEDIUM | Homegrown sanitizer vs DOMPurify | Hardened; library swap is a dedicated dependency decision |
| D3 | LOW | Nested live regions on toasts | Works; simplify when redesigning notifications |
| D4 | LOW | Placeholder Settings/Profile routes | Product placeholders |
| D5 | LOW | Unused exports (`getTokenExpiryIso`, etc.) | Harmless |
| D6 | LOW | Repeated `URLSearchParams` builders in API modules | Style only |

---

## Area notes

### Components / composables / stores

Composable-driven forms and lifecycle actions are consistent. Only three Pinia stores (auth, notifications, UI loading) — appropriate global state. No evidence of unnecessary domain stores.

### API layer

Typed DTOs and Problem Details mapping are solid. Upload XHR remains for progress reporting but now shares refresh semantics.

### Routing / authz UI

Guards enforce auth + permissions. Mutation routes now require update permissions. UI still disables controls where users can reach a page; backend remains authoritative.

### TypeScript

Strict mode is on. Residual `unknown` for error bodies is intentional. Preview view still casts field types from public API payloads (acceptable).

### Accessibility

Meets the package a11y doc for core flows. Rich text remains the weakest interactive surface (platform limits).

### Performance

Lazy routes, no heavy Pinia domain caches. Module-level media/relation caches are intentional with logout eviction. No systemic excessive-rerender pattern found in review.

### Security

SessionStorage tokens + XSS defense-in-depth (sanitizer + no `v-html` in product UI). Open redirect closed. Soft trust: JWT permissions are UI-only.

### Tests

Vitest covers stores, API, views, guards, sanitizer, a11y primitives. Playwright covers auth, authorization, content lifecycle, media, versioning, concurrency, a11y. Fragile CSS form locators remain a maintenance risk for E2E.

---

## Verification

Commands run from `frontend/contentforge-web` (Node 20):

```bash
npm run format:check
npm run lint
npm run typecheck
npm run test
npm run build:vite
# E2E (repo root):
docker compose -f docker-compose.e2e.yml up -d --build
npm run test:e2e
```

Unit/component: **94 passed**. Production Vite build: **pass**. E2E: **16 passed** against Compose stack on ports 28080/25080.

---

## Recommended next work

1. Replace or wrap rich-text sanitizer with a maintained library (DOMPurify) after dependency review.  
2. Abort/generation-guard list fetches.  
3. Migrate E2E form locators to roles/`getByLabel`.  
4. Update `docs/ARCHITECTURE.md` §5 to match the real frontend tree.  
5. Collapse permission composables into one factory.

---

## Conclusion

The frontend is **production-capable** for ContentForge after this remediation: auth redirect hardening, cache hygiene, sanitizer/editor sync fixes, upload refresh parity, and stricter edit-route permissions. Remaining items are evolutionary improvements, not blockers for a first production release behind TLS and a secured API.
