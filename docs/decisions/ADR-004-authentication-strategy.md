# ADR-004: Authentication Strategy (JWT + Identity)

**Status:** Accepted  
**Date:** 2026-08-29

## Context

The product requires authenticated admin APIs, role/permission authorization, and refreshable sessions without inventing a custom user store.

## Decision

- Use **ASP.NET Core Identity** for user accounts, password hashing, and password-reset tokens
- Issue short-lived **JWT access tokens** (role + permission claims) validated by JwtBearer middleware
- Issue opaque **refresh tokens** persisted server-side; refresh rotates/claims tokens under concurrency controls
- Authorize via **permission policies** (`PermissionAuthorizationHandler`), not role checks alone
- Frontend stores tokens in **sessionStorage** and refreshes on 401

## Consequences

- Standard Identity security features; backend remains authoritative for AuthZ
- JWT permissions are for UI/API convenience — always re-checked server-side
- Signing key must be strong and, in Azure, stored in Key Vault (`jwt-signing-key`)
