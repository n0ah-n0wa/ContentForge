# ADR-002: PostgreSQL Locally / Azure SQL in Production

**Status:** Accepted  
**Date:** 2026-08-29

## Context

The specification requires PostgreSQL for development and Azure SQL Database for production (SPEC §5.3, §74). EF Core must abstract database-specific implementation wherever practical.

## Decision

- **Development / Test / CI:** PostgreSQL via Docker Compose
- **Staging / Production:** Azure SQL Database
- Use EF Core as the primary abstraction; isolate provider-specific SQL in Infrastructure
- Migrations tested against PostgreSQL in CI; validated for Azure SQL compatibility before production deployment

## Consequences

- Developers use Docker Compose for local database infrastructure
- Some query features (JSON operators, full-text search) may require provider-conditional implementations
- CI must run integration tests against PostgreSQL
