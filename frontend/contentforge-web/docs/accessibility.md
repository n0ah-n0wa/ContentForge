# Frontend accessibility (WCAG 2.1 AA)

ContentForge admin UI targets **reasonable WCAG 2.1 Level AA** compliance for keyboard, screen-reader, and contrast basics.

## What we enforce

| Layer | Mechanism                                                                             |
| ----- | ------------------------------------------------------------------------------------- |
| Lint  | `eslint-plugin-vuejs-accessibility` (selected rules in `eslint.config.js`)            |
| Unit  | `axe-core` via `tests/support/a11y.ts` (`tests/unit/accessibility.test.ts`)           |
| E2E   | `@axe-core/playwright` smoke on login + dashboard (`e2e/specs/accessibility.spec.ts`) |

## Product patterns

- Skip links on app shell and auth layout
- Modal dialogs: focus entry, Tab trap, Escape dismiss, restore focus (`useModalDialog`)
- Form errors: `aria-invalid` + `aria-describedby` / `role="alert"` where wired
- Status: textual labels + `role="status"` / live regions (not color alone)
- Tables: `<th scope="col">` on list views
- Notifications: error toasts use `role="alert"` / assertive live region

## Known limitations

1. Rich-text editing still relies on `contenteditable` / `document.execCommand` — toolbar semantics are improved, but a full editor a11y suite is out of scope.
2. Some dense CMS forms still use wrapping `<label class="form-field">` without separate `id`/`for` pairs; migration is incremental.
3. Disabled controls use opacity and may fall below AA text contrast (expected for disabled state).
4. axe E2E covers login + dashboard only; deeper journeys are not axe-gated yet.
5. Color themes are light-mode only; no dark-theme AA audit.

## Manual checklist (release)

- [ ] Tab through login, dashboard, content editor, media picker, user list
- [ ] Escape closes confirm dialogs and media picker; focus returns
- [ ] Screen reader announces validation errors and status changes
- [ ] Zoom to 200% — primary workflows remain usable
