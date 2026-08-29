# ContentForge — AI Agent Guide

This repository is developed primarily with AI coding agents. Read these documents before making changes:

1. **[SPECIFICATIONS.md](./SPECIFICATIONS.md)** — authoritative product and engineering requirements
2. **[docs/DEVELOPMENT_RULES.md](./docs/DEVELOPMENT_RULES.md)** — coding, testing, and agent operating rules
3. **[docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)** — system architecture and dependency boundaries
4. **[docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md)** — phased delivery plan

## Quick Rules

- Do not silently redefine requirements from `SPECIFICATIONS.md`
- Respect Clean Architecture dependency direction (Domain has no outward dependencies)
- Implement incrementally; run verification before declaring work complete
- Behavior changes require tests
- Do not suppress warnings or skip tests to pass CI

## Verification Commands

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes

cd frontend/contentforge-web
npm run lint
npm run test
npm run build
```
