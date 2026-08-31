# ContentForge Authentication & Authorization — Security Review

**Date:** 2026-08-29  
**Scope:** Identity, JWT, refresh tokens, RBAC policies, password handling, configuration  
**Authority:** [SPECIFICATIONS.md](../../SPECIFICATIONS.md) §29–§31, [ARCHITECTURE.md](../ARCHITECTURE.md) §7–§8

---

## Executive Summary

A security review of authentication and authorization identified **concrete vulnerabilities** that would allow weak passwords into the system, forged JWTs if committed secrets were deployed, continued API access after logout/disable, and incomplete refresh-token theft mitigation.

All listed vulnerabilities were **fixed in this pass**, with regression tests. Server-side authorization already prevented Author→Administrator escalation and Viewer mutations when calling APIs directly (frontend is not a security boundary).

**Verification after fixes:**

| Check | Result |
|-------|--------|
| `dotnet build --configuration Release` | 0 warnings, 0 errors |
| Unit tests | 96 passed |
| Architecture tests | 26 passed |
| Integration tests | 53 passed (includes security regression suite) |

---

## Findings & Remediation

### Critical

| ID | Finding | Risk | Fix |
|----|---------|------|-----|
| C1 | `CreateUserCommandValidator` was never invoked; Identity password options unused on create path | Admins (or future callers) could create users with weak passwords (e.g. `aaaaaaaaaaaa`) | `CommandValidator.EnsureValidAsync` in `CreateUserCommandHandler`; `PasswordRules` mirrors Identity complexity |
| C2 | JWT signing key and DB password committed in base `appsettings.json` | Repo compromise → forge JWTs / connect to misconfigured deployments | Secrets removed from base config; Development/Testing only; Production rejects `DEV_ONLY`/`TEST_ONLY`/`CHANGE_ME` keys |

### High

| ID | Finding | Risk | Fix |
|----|---------|------|-----|
| H1 | Disabled users retained valid access JWTs until expiry | Stolen/leftover Bearer token worked after disable | `OnTokenValidated` checks `IsActive`; disable calls `ISessionInvalidationService` (revoke refresh + bump security stamp) |
| H2 | Logout only revoked refresh tokens | Access JWT remained valid after logout | Logout revokes all refresh tokens **and** updates security stamp; JWT embeds `sstamp` claim validated on each request |
| H3 | Refresh rotation without reuse detection | Stolen refresh token reuse left victim session intact | Reuse of revoked refresh token → revoke **all** user refresh tokens + bump security stamp |

### Medium

| ID | Finding | Risk | Fix |
|----|---------|------|-----|
| M1 | Access token lifetime 60 minutes amplified H1–H3 | Long stolen-token window | Default / config reduced to **15 minutes** |
| M2 | Password validator only checked length | Complexity weaker than Identity | Digit, upper, lower, non-alphanumeric required |
| M3 | No CORS allowlist | Spec §51; risk when SPA ships with credentials | Named CORS policy from `Cors:AllowedOrigins` (no `AllowAnyOrigin`) |
| M4 | No explicit JWT algorithm allowlist | Defense in depth vs alg confusion | `ValidAlgorithms = [HmacSha256]`; `RequireHttpsMetadata` in Production |

### High (2026-08-31 audit)

| ID | Finding | Risk | Fix |
|----|---------|------|-----|
| H4 | Rich Text stored without server-side sanitization | Stored XSS via direct API → public consumers | `RichTextSanitizer` + coerce/public mapping |
| H5 | Role change without session invalidation | Demoted users retain elevated JWT up to 15 min | `ISessionInvalidationService` in `UpdateUserCommandHandler` |

### Medium (2026-08-31 audit)

| ID | Finding | Risk | Fix |
|----|---------|------|-----|
| M5 | No rate limiting on `/auth/login` and `/auth/refresh` | Distributed credential stuffing | `"auth"` IP rate limit policy (20/min default) |

### Low / Informational (no change required)

| ID | Finding | Assessment |
|----|---------|------------|
| L1 | Password hashing | ASP.NET Identity PBKDF2-SHA512 with per-password salt — **OK** |
| L2 | Account lockout | 5 failures / 15 min; `LockoutEnabled` on users; `AccessFailedAsync` — **OK** |
| L3 | Author cannot create users / assign Administrator | Policy `user.create` + handler — **OK** (regression test added) |
| L4 | Viewer cannot mutate content | Policy + domain permissions — **OK** |
| L5 | Sensitive logging | No passwords/tokens/secrets logged — **OK** |
| L6 | Claim spoofing | Claims only from validated JWT — **OK** |
| L7 | API callable without frontend | **By design**; authz is server-side |

---

## Privilege Escalation Analysis

| Attack | Result after review |
|--------|---------------------|
| Call `POST /api/v1/users` as Author with `role: Administrator` | **403** — missing `user.create` |
| Call `POST /api/v1/content/{id}/publish` as Author | **403** — missing `content.publish` |
| Call `POST /api/v1/content` as Viewer | **403** — missing `content.create` |
| Forge JWT without signing key | Blocked by signature validation |
| Use JWT after logout/disable | **401** — security stamp / IsActive check |
| Reuse rotated refresh token | **401** + full session revocation |
| Create user with `aaaaaaaaaaaa` | **400** — password policy |

Malicious clients cannot bypass frontend restrictions by calling APIs directly: every protected mutation is gated by JWT authentication, permission policies, and Application-layer `ApplicationGuard`.

---

## Configuration & Secret Handling

| Environment | JWT signing key | DB connection |
|-------------|-----------------|---------------|
| Base `appsettings.json` | Empty (must be supplied) | Empty |
| Development | Local `DEV_ONLY_*` placeholder | Local Docker defaults |
| Testing | Local `TEST_ONLY_*` placeholder | Test DB |
| Production | **Required** via env / Key Vault; placeholders rejected at startup | Required via env / Key Vault |

Recommended production settings:

```bash
Jwt__SigningKey=<32+ char high-entropy secret>
Jwt__Issuer=ContentForge
Jwt__Audience=ContentForge.Admin
Jwt__AccessTokenLifetimeMinutes=15
Database__ConnectionString=<Azure SQL / secret store>
Cors__AllowedOrigins__0=https://admin.example.com
```

---

## Token Strategy (post-fix)

1. **Access JWT** — HMAC-SHA256; issuer/audience/lifetime/signing key validated; claims include role, permissions, and `sstamp`.
2. **Per-request binding** — `OnTokenValidated` loads user; rejects if inactive or stamp mismatch.
3. **Refresh tokens** — stored hashed; rotated on use; reuse of revoked token invalidates the whole family.
4. **Logout / disable** — revoke all refresh tokens + rotate security stamp (invalidates outstanding access JWTs).

---

## Regression Tests Added

| Test | Guards |
|------|--------|
| `CreateUser_WithWeakPassword_Returns400` | C1 / M2 |
| `DisabledUser_AccessTokenIsRejectedImmediately` | H1 |
| `Logout_InvalidatesAccessToken` | H2 |
| `RefreshTokenReuse_RevokesTokenFamily` | H3 |
| `Author_CannotEscalateByCreatingAdministrator` | privilege escalation |
| Unit: strong vs letter-only password validators | C1 / M2 |

Existing authorization integration tests continue to cover 401/403 role matrices.

---

## Remaining Recommendations (deferred)

1. **HTTP rate limiting** on `/auth/login` and password-reset endpoints (SPEC §49) — Identity lockout mitigates credential stuffing partially.
2. **Azure Key Vault** for production JWT signing keys and connection strings.
3. **ADR-004** — document JWT + Identity + refresh reuse strategy formally.
4. **Password reset delivery** — current abstraction generates tokens but does not send email (out of scope for this review).

---

## Files Changed

| Area | Files |
|------|--------|
| Validation | `CommandValidator.cs`, `PasswordRules.cs`, `UserCommands.cs` |
| Sessions | `ISessionInvalidationService.cs`, `IdentitySessionInvalidationService.cs` |
| Auth | `IdentityAuthenticationService.cs`, `RefreshTokenService.cs`, `JwtTokenService.cs`, `DependencyInjectionAuthentication.cs` |
| API | `Program.cs` (CORS), `UsersController` (disable), `appsettings*.json` |
| Tests | `SecurityAuthRegressionTests.cs`, password validator unit tests |
| Docs | this file |
