# ContentForge

Production-oriented headless Content Management System built with **.NET 8**, **Vue 3**, and **Microsoft Azure**.

## Documentation

- [SPECIFICATIONS.md](./SPECIFICATIONS.md) — authoritative product and engineering specification
- [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) — system architecture
- [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md) — phased implementation roadmap
- [docs/DEVELOPMENT_RULES.md](./docs/DEVELOPMENT_RULES.md) — coding and agent rules

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local infrastructure)

## Quick Start

### Backend

```bash
dotnet restore
dotnet build
dotnet test
```

Run the API:

```bash
dotnet run --project src/ContentForge.Api
```

Swagger UI: `https://localhost:7080/swagger` (Development)

### Frontend

```bash
cd frontend/contentforge-web
npm install
npm run dev
```

### Local Infrastructure

```bash
docker compose up -d
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
