# ContentForge API

**Base path:** `/api/v1`  
**OpenAPI:** Development/Testing only — Swagger UI at `/swagger`, document at `/swagger/v1/swagger.json`  
**Errors:** Problem Details–style JSON (validation → 422, auth → 401, forbidden → 403, concurrency → 409, rate limit → 429)  
**Health (unversioned):** `GET /health/live`, `GET /health/ready`  
**Media binaries (unversioned):** `GET /media-files/{**storageKey}`

Authoritative contract: run the API and use Swagger, or inspect controllers under `src/ContentForge.Api/Controllers/`. This page summarizes routes and shows copy-paste examples that match the implemented DTOs.

---

## Authentication

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| POST | `/api/v1/auth/login` | Anonymous | Rate limited |
| POST | `/api/v1/auth/refresh` | Anonymous | Body: `{ "refreshToken" }` |
| POST | `/api/v1/auth/logout` | Bearer | Optional `{ "refreshToken" }` |
| GET | `/api/v1/auth/me` | Bearer | Current user |
| POST | `/api/v1/auth/forgot-password` | Anonymous | Rate limited |
| POST | `/api/v1/auth/reset-password` | Anonymous | Email + reset token + new password |

Send access tokens as:

```http
Authorization: Bearer <accessToken>
```

### Example — login

```bash
curl -s -X POST "http://localhost:5080/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@contentforge.local\",\"password\":\"AdminPassword123!\"}"
```

Example success shape (`LoginResultDto`):

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "email": "admin@contentforge.local",
  "displayName": "Administrator",
  "role": "Administrator",
  "accessToken": "<jwt>",
  "accessTokenExpiresAt": "2026-09-05T12:00:00+00:00",
  "refreshToken": "<opaque>",
  "refreshTokenExpiresAt": "2026-09-12T12:00:00+00:00"
}
```

### Example — refresh

```bash
curl -s -X POST "http://localhost:5080/api/v1/auth/refresh" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"<refreshToken>\"}"
```

---

## Route map

### Users & roles

| Method | Path | Typical permission |
|--------|------|-------------------|
| GET | `/api/v1/users` | `user.read` |
| GET | `/api/v1/users/{id}` | `user.read` |
| POST | `/api/v1/users` | `user.create` |
| PUT | `/api/v1/users/{id}` | `user.update` |
| POST | `/api/v1/users/{id}/disable` | `user.disable` |
| POST | `/api/v1/users/{id}/enable` | `user.disable` |
| GET | `/api/v1/roles` | `user.read` |

### Content types

| Method | Path | Permission (typical) |
|--------|------|----------------------|
| GET/POST | `/api/v1/content-types` | `contentType.read` / `create` |
| GET/PUT/DELETE | `/api/v1/content-types/{id}` | read / update / delete |
| POST | `/api/v1/content-types/{id}/fields` | update |
| PUT/DELETE | `/api/v1/content-types/{id}/fields/{fieldName}` | update |
| PUT | `/api/v1/content-types/{id}/fields/{fieldName}/name` | update (rename) |
| POST | `/api/v1/content-types/{id}/deactivate` | update |

### Content entries (admin)

| Method | Path | Notes |
|--------|------|-------|
| GET | `/api/v1/content` | Filter/sort/page |
| GET | `/api/v1/content/search` | Keyword search |
| GET/POST | `/api/v1/content`, `/api/v1/content/{id}` | CRUD |
| PUT/DELETE | `/api/v1/content/{id}` | Update requires `concurrencyToken` |
| POST | `.../submit-for-review`, `withdraw-from-review`, `publish`, `unpublish`, `archive`, `restore` | Lifecycle |
| PUT/DELETE | `.../schedule` | Scheduled publish/unpublish |
| POST | `.../preview-token` | Issue preview token |
| GET | `/api/v1/content/preview/{token}` | Preview payload |
| GET | `.../versions`, `.../versions/{n}`, `.../versions/compare` | History |
| POST | `.../versions/{n}/restore` | Restore version |

### Public API

| Method | Path | Auth |
|--------|------|------|
| GET | `/api/v1/public/{contentTypeSlug}` | Anonymous (published list) |
| GET | `/api/v1/public/{contentTypeSlug}/{slug}` | Anonymous (published entry) |

### Media, audit, dashboard

| Method | Path |
|--------|------|
| GET/POST | `/api/v1/media` |
| GET/PUT/DELETE | `/api/v1/media/{id}` |
| GET | `/media-files/{**storageKey}` |
| GET | `/api/v1/audit`, `/api/v1/audit/{id}` |
| GET | `/api/v1/dashboard` |

---

## Examples

Replace `$TOKEN` with an access token. Examples assume Docker API on port **5080**.

### Create a content type

```bash
curl -s -X POST "http://localhost:5080/api/v1/content-types" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"article\",
    \"displayName\": \"Article\",
    \"slug\": \"article\",
    \"description\": \"Blog articles\"
  }"
```

### Add fields

```bash
curl -s -X POST "http://localhost:5080/api/v1/content-types/<content-type-guid>/fields" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"title\",
    \"fieldType\": \"Text\",
    \"displayName\": \"Title\",
    \"sortOrder\": 0,
    \"configuration\": {
      \"isRequired\": true,
      \"allowMultiple\": false,
      \"options\": []
    }
  }"
```

`FieldType` values: `Text`, `LongText`, `RichText`, `Integer`, `Decimal`, `Boolean`, `Date`, `DateTime`, `Media`, `MediaMultiple`, `Relation`, `RelationMultiple`, `Select`, `MultiSelect`, `Json`. Relation fields require `relationTarget` and `relationCardinality` in `configuration`. Prefer Swagger for complex configs.

### Create a draft entry

```bash
curl -s -X POST "http://localhost:5080/api/v1/content" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"contentTypeId\": \"<content-type-guid>\",
    \"slug\": \"hello-world\",
    \"data\": {
      \"title\": \"Hello world\",
      \"body\": \"First article\"
    }
  }"
```

### Submit for review, then publish

Publish requires `content.publish` (typically Editor+) and a current `concurrencyToken` (`uint`). Entries usually move Draft → In review → Published.

```bash
curl -s -X POST "http://localhost:5080/api/v1/content/<entry-guid>/submit-for-review" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"concurrencyToken\": 1}"

curl -s -X POST "http://localhost:5080/api/v1/content/<entry-guid>/publish" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"concurrencyToken\": 2,
    \"changeSummary\": \"Initial publish\"
  }"
```

### Public read

```bash
curl -s "http://localhost:5080/api/v1/public/article/hello-world"
```

### List media

```bash
curl -s "http://localhost:5080/api/v1/media?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

### Upload media (multipart)

Form fields: `file` (required), optional `altText`, `title`, `description`.

```bash
curl -s -X POST "http://localhost:5080/api/v1/media" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@./image.png" \
  -F "altText=Hero image"
```

---

## Roles and permissions (summary)

| Role | Intent |
|------|--------|
| Administrator | All permissions |
| Editor | Content CRUD, publish/archive/restore/review, versions, media; content-type **read** only |
| Author | Content create/update/review (own-content rules apply), version read, media upload; **no** `content.publish` |
| Viewer | Read content types, content, media, and versions |

Permission strings include `content.*`, `contentType.*`, `media.*`, `user.*`, `audit.read`, version restore permissions, etc. Defaults live in `ContentForge.Domain` (`RolePermissions` / `DefaultRoleDefinitions`). The API enforces policies; the admin UI mirrors them for UX.

---

## Concurrency

Mutating content endpoints accept a `concurrencyToken` (`uint`). Stale tokens produce **409 Conflict**. Clients must reload and retry.

---

## Related

- [ARCHITECTURE.md](../ARCHITECTURE.md)
- [Rate limiting](../architecture/rate-limiting.md)
- [Security auth review](../architecture/security-auth-review.md)
- [API architecture review](../architecture/api-review.md)
