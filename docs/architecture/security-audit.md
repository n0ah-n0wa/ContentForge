# ContentForge — Comprehensive Security Audit

**Date:** 2026-08-31  
**Scope:** Full application — authentication, authorization, API, media, public content, frontend, configuration, dependencies  
**Prior review:** [security-auth-review.md](./security-auth-review.md) (2026-08-29 identity pass)

---

## Executive summary

ContentForge enforces **server-side authorization** on all administrative mutations. The August 2026 identity review fixed password policy, JWT/session handling, CORS, and refresh-token reuse detection.

This audit extended coverage to **stored XSS**, **stale JWT permissions after role change**, and **auth endpoint rate limiting**. Three concrete vulnerabilities were fixed with regression tests. Remaining items are documented as accepted risks or deferred hardening.

---

## Threat model (attacker goals)

| Goal | Mitigation status |
|------|-------------------|
| Access another user's content | **Blocked** — `ApplicationGuard.EnsureCanReadContent` / `EnsureCanModifyContent` |
| Publish without permission | **Blocked** — `content.publish` policy + handler guards |
| Modify another user's content | **Blocked** — ownership checks on update/delete/lifecycle |
| Bypass lifecycle restrictions | **Blocked** — domain state machine + permission policies |
| Retrieve drafts via public API | **Blocked** — `PublishedRepresentationOnly`, 404 for non-published |
| Upload malicious files | **Blocked** — magic-byte validation, SVG script rejection, size limits |
| Abuse authentication endpoints | **Mitigated** — lockout + IP rate limiting on login/refresh (this pass) |
| Exploit concurrency | **Blocked** — optimistic concurrency required; stale tokens → 409 |
| Extract secrets from repo/config | **Mitigated** — empty base config; production rejects placeholder keys |
| Stored XSS via direct API | **Fixed this pass** — server-side Rich Text sanitization |
| Retain elevated JWT after demotion | **Fixed this pass** — session invalidation on role change |

---

## Findings fixed in this audit

### H1 — Stored XSS via Rich Text (API bypass)

**Severity:** High  
**Issue:** RichText fields were validated as plain strings only. An attacker with content-create permission could POST malicious HTML (`<script>`, `onerror` handlers) directly to the API, bypassing frontend sanitization. Published content served via `/api/v1/public/...` could deliver XSS to headless consumers rendering HTML.

**Fix:**
- `RichTextSanitizer` (HtmlSanitizer 9.0.892, allowlist aligned with `richTextSanitizer.ts`)
- Sanitize on create/update in `ContentDataCoercer`
- Defense in depth on public read in `ContentEntryMapper.ToPublicDto`

**Tests:** `RichTextSanitizerTests`, `RichTextSecurityIntegrationTests`

---

### H2 — Stale JWT permissions after role change

**Severity:** High  
**Issue:** `UpdateUserCommandHandler` changed roles without invalidating sessions. A demoted Editor's access JWT retained Editor permissions until expiry (up to 15 minutes).

**Fix:** Call `ISessionInvalidationService.InvalidateUserSessionsAsync` when `previousRole != user.Role` (same mechanism as disable/logout).

**Tests:** `RoleChange_InvalidatesExistingAccessToken`, `UpdateUserCommandHandler_RoleChange_InvalidatesSessions`

---

### M1 — Missing rate limiting on auth endpoints

**Severity:** Medium  
**Issue:** `/api/v1/auth/login` and `/auth/refresh` had no IP-based rate limits. Per-account lockout (5 failures / 15 min) does not stop distributed credential stuffing.

**Fix:** `"auth"` rate-limit policy (20 req/min/IP production; configurable via `RateLimiting:AuthPermitLimit`).

**Tests:** `AuthRateLimitIntegrationTests`

---

## Previously fixed (auth review — still verified)

| ID | Issue | Status |
|----|-------|--------|
| C1 | Weak passwords on user create | Fixed — FluentValidation + `PasswordRules` |
| C2 | Secrets in base appsettings | Fixed — empty base; env-only in production |
| H1–H3 | Disabled/logout token validity, refresh reuse | Fixed — security stamp + token family revocation |
| M1–M4 | Token lifetime, password complexity, CORS, JWT algorithms | Fixed |

See [security-auth-review.md](./security-auth-review.md) for details and regression test matrix.

---

## Areas reviewed — no new fix required

### Authorization & IDOR

All administrative controllers require `[Authorize]`. Public and preview endpoints are intentionally anonymous with strict data scoping. Content preview uses opaque hashed tokens with expiry and revocation (`ContentPreviewSecurityIntegrationTests`).

### SQL injection

EF Core parameterized queries only. Search uses escaped `ILike` patterns (`PortableSearch`). Raw SQL limited to parameterized scheduled-job updates.

### Mass assignment

API uses explicit request records; no direct entity binding from JSON.

### File upload & path traversal

Magic-byte inspection, extension validation, null-byte rejection, storage key sanitization (`MediaSecurityApiIntegrationTests` — 14 tests).

### Public content & caching

Only `PublicContentDto` cached after visibility checks. Invalidation on lifecycle events (`PublicContentCacheIntegrationTests`).

### CSRF

Administrative API uses JWT Bearer tokens (not cookie sessions). CSRF against the API is not applicable; CORS allowlist restricts browser origins when configured.

### Error responses & information disclosure

Unhandled exceptions return generic 500 ProblemDetails. Validation errors return field-level messages only. Sensitive telemetry redacted.

### Logging & audit

Authentication failures do not log passwords or tokens. Administrative mutations recorded in audit log.

### Frontend token storage

Access and refresh tokens stored in `sessionStorage` (tab-scoped, cleared on close) — not `localStorage`.

### Concurrency

Domain enforces version match; API returns 409 on conflict. No bypass identified.

### Dependencies

`HtmlSanitizer` pinned to **9.0.892** (patched for CVE-2026-25543 / GHSA-j92c-7v7g-gj3f). `dotnet restore` treats known vulnerable packages as build errors.

---

## Deferred recommendations

| Item | Priority | Notes |
|------|----------|-------|
| Azure Key Vault for production secrets | Medium | Operational |
| Distributed rate limiting (Redis) | Medium | Multi-instance deployments |
| Admin list summary DTO | Low | Reduce payload size, not a direct vuln |
| PDF active-content scanning | Low | Standard CMS trade-off |
| Formal ADR for JWT strategy | Low | Documentation |
| Password reset email delivery | Low | Out of scope |

---

## Verification

| Command | Result |
|---------|--------|
| `dotnet build` | Pass |
| `dotnet test` | Pass (includes new security regression tests) |
| `dotnet format --verify-no-changes` | Pass |
| `npm run lint` / `npm run test` / `npm run build` | Pass |

### New regression tests (this audit)

| Test | Guards |
|------|--------|
| `RichTextSanitizerTests` | Script/event handler/javascript URL stripping |
| `RichTextSecurityIntegrationTests` | API → publish → public endpoint XSS |
| `RoleChange_InvalidatesExistingAccessToken` | JWT permission staleness |
| `UpdateUserCommandHandler_RoleChange_InvalidatesSessions` | Handler unit coverage |
| `AuthRateLimitIntegrationTests.Login_ExceedingAuthRateLimit_Returns429` | Auth abuse |

---

## Configuration reference

```bash
# Production essentials
Jwt__SigningKey=<32+ char high-entropy secret>
Jwt__AccessTokenLifetimeMinutes=15
Cors__AllowedOrigins__0=https://admin.example.com
RateLimiting__AuthPermitLimit=20   # optional override
```
