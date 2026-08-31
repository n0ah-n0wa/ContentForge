# ContentForge

Production-oriented headless Content Management System built with **.NET 8**, **Vue 3**, and **Microsoft Azure**.

## Documentation

- [SPECIFICATIONS.md](./SPECIFICATIONS.md) — authoritative product and engineering specification
- [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) — system architecture
- [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md) — phased implementation roadmap
- [docs/DEVELOPMENT_RULES.md](./docs/DEVELOPMENT_RULES.md) — coding and agent rules
- [docs/operations/docker.md](./docs/operations/docker.md) — production container deployment

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — optional if using Docker only
- [Node.js 20+](https://nodejs.org/) — optional if using Docker only
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — **required for the Docker workflow below**

## Quick Start (Docker — recommended for new developers)

From the **repository root**, start the full local stack (PostgreSQL, Azurite, API, admin UI):

```bash
docker compose up -d --build
```

| Service | URL |
|---------|-----|
| **Admin UI** | http://localhost:8080 |
| **API (Swagger)** | http://localhost:5080/swagger |
| **PostgreSQL** | `localhost:5432` (user/password/db: `contentforge`) |
| **Azurite blob** | http://localhost:10000 |

**Default sign-in** (seeded automatically on first start in Development):

| Email | Password |
|-------|----------|
| `admin@contentforge.local` | `AdminPassword123!` |

The API applies EF Core migrations and creates the administrator account when the database is empty. Persistent data is stored in Docker volumes (`postgres_data`, `api_media`, `azurite_data`).

### Useful commands

```bash
# Follow logs
docker compose logs -f api web

# Stop services (keep data)
docker compose down

# Reset everything and start fresh
docker compose down -v
docker compose up -d --build

# Infrastructure only (PostgreSQL + Azurite) for host-based dotnet/npm dev
docker compose up -d postgres azurite
```

Optional environment overrides: copy [`infra/docker/.env.development.example`](./infra/docker/.env.development.example) to `.env.development` and run `docker compose --env-file .env.development up -d --build`.

Production-style containers (separate from this dev stack): see [`infra/docker/README.md`](./infra/docker/README.md).

## Native development (without Docker app containers)

Use this when iterating on backend or frontend with hot reload.

### Backend

```bash
dotnet restore
dotnet build
dotnet test
```

Start PostgreSQL and Azurite first:

```bash
docker compose up -d postgres azurite
dotnet run --project src/ContentForge.Api
```

Swagger UI: `https://localhost:7080/swagger` (Development)

### Frontend

```bash
cd frontend/contentforge-web
npm install
npm run dev
```

Vite dev server: http://localhost:5173 (proxies `/api` to the API on port 5080)

## Verification (CI parity)

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes

cd frontend/contentforge-web
npm ci
npm run lint
npm run test
npm run build
```

## Repository Structure

```text
src/           — .NET backend (Clean Architecture)
frontend/      — Vue 3 admin SPA
tests/         — unit, integration, and architecture tests
docs/          — architecture, API, ADRs, operations
infra/         — Docker and Azure deployment assets
```

## License

Private — portfolio / demonstration project.
