# ContentForge — AI Agent Guide

This repository is developed primarily with AI coding agents. Read these documents before making changes:

1. **[SPECIFICATIONS.md](./SPECIFICATIONS.md)** — authoritative product and engineering requirements
2. **[docs/DEVELOPMENT_RULES.md](./docs/DEVELOPMENT_RULES.md)** — coding, testing, and agent operating rules
3. **[docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)** — system architecture and dependency boundaries (as built)
4. **[docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md)** — phased delivery record (complete)
5. **[docs/operations/](./docs/operations/)** — CI/CD, Azure, migrations, backups
6. **[docs/decisions/](./docs/decisions/)** — Architecture Decision Records

## Quick Rules

- Do not silently redefine requirements from `SPECIFICATIONS.md`
- Respect Clean Architecture dependency direction (Domain has no outward dependencies)
- Implement incrementally; run verification before declaring work complete
- Behavior changes require tests
- Do not suppress warnings or skip tests to pass CI
- Do not document or claim features that are not implemented

## Verification Commands

Matches the [CI pipeline](docs/operations/ci.md) (`.github/workflows/ci.yml`). Order matches CI: format before build; `dotnet test --no-build` requires the Release build step.

```bash
dotnet restore
dotnet format ContentForge.sln --verify-no-changes
dotnet build ContentForge.sln --configuration Release
dotnet test --configuration Release --no-build

cd frontend/contentforge-web
npm ci
npm audit --audit-level=moderate
npm run format:check
npm run lint
npm run typecheck
npm run test
npm run build:vite

# E2E (requires docker-compose.e2e.yml stack — see frontend/contentforge-web/e2e/README.md)
# docker compose -f docker-compose.e2e.yml up -d --build
# npm run e2e:install && npm run test:e2e
```
