# Rate limiting

ContentForge applies ASP.NET Core fixed-window rate limiting to anonymous and abuse-sensitive endpoints. Administrative JWT-authenticated CRUD routes are **not** rate limited so editors and administrators can work without artificial throttling.

Implementation: `src/ContentForge.Api/Infrastructure/RateLimiting/`

---

## Policies

| Policy | Endpoints | Partition | Default limit |
|--------|-----------|-----------|---------------|
| `auth-login` | `POST /api/v1/auth/login` | Client IP | 15 / minute |
| `auth-refresh` | `POST /api/v1/auth/refresh` | Client IP | 60 / minute |
| `password-reset-request` | `POST /api/v1/auth/forgot-password` | Client IP | 5 / minute |
| `password-reset-confirm` | `POST /api/v1/auth/reset-password` | Client IP | 10 / minute |
| `public-api` | `GET /api/v1/public/*` | Client IP | 120 / minute |
| `media-upload` | `POST /api/v1/media` | Authenticated user (`sub`), else IP | 30 / minute |
| `content-preview` | `GET /api/v1/content/preview/{token}` | Client IP | 60 / minute |

Limits are per partition key within a sliding fixed window (`WindowSeconds`, default 60).

---

## HTTP 429 response

When a limit is exceeded:

- Status: **429 Too Many Requests**
- Body: RFC 7807 Problem Details (`application/problem+json`)
- Type: `https://contentforge/errors/rate-limit`
- Header: **`Retry-After`** (seconds until the window resets) when metadata is available

Example:

```json
{
  "type": "https://contentforge/errors/rate-limit",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Retry later.",
  "instance": "/api/v1/auth/login",
  "traceId": "...",
  "correlationId": "..."
}
```

---

## Configuration

All settings live under the `RateLimiting` section in `appsettings.json` (or environment variables).

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | When `false`, all policies use `DisabledPermitLimit` |
| `DisabledPermitLimit` | `100000` | Effectively unlimited for normal traffic |
| `{PolicyName}:PermitLimit` | see table | Max requests per window per partition |
| `{PolicyName}:WindowSeconds` | `60` | Fixed window duration |
| `MediaUpload:PartitionByAuthenticatedUser` | `true` | Partition uploads by JWT user id |

Policy names in configuration match the `RateLimitingOptions` properties: `AuthLogin`, `AuthRefresh`, `PasswordResetRequest`, `PasswordResetConfirm`, `PublicApi`, `MediaUpload`, `ContentPreview`.

### Example — production overrides

```bash
RateLimiting__Enabled=true
RateLimiting__AuthLogin__PermitLimit=15
RateLimiting__AuthRefresh__PermitLimit=60
RateLimiting__PasswordResetRequest__PermitLimit=5
RateLimiting__PasswordResetConfirm__PermitLimit=10
RateLimiting__PublicApi__PermitLimit=120
RateLimiting__MediaUpload__PermitLimit=30
RateLimiting__ContentPreview__PermitLimit=60
```

### Testing environment

When `ASPNETCORE_ENVIRONMENT=Testing`, policies use `DisabledPermitLimit` (10 000) unless a specific `PermitLimit` override is supplied in configuration. Integration tests set low overrides to assert 429 behaviour.

To disable rate limiting entirely (e.g. local load testing):

```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

---

## Design notes

- **Login vs refresh** use separate policies so legitimate token refresh traffic is not blocked by login throttling.
- **Password reset** endpoints always return generic outcomes (204 / 401) to prevent account enumeration; rate limits add IP-level abuse protection.
- **Media upload** partitions by authenticated user so one editor cannot exhaust another’s quota.
- **Administrative routes** (`/content`, `/users`, `/content-types`, etc.) rely on JWT authentication and RBAC instead of rate limits.

---

## Verification

Integration tests: `tests/ContentForge.IntegrationTests/Security/RateLimitIntegrationTests.cs`

Covers login, refresh, forgot-password, public API, content preview 429 responses, and confirms authenticated admin `/auth/me` calls are not throttled.
