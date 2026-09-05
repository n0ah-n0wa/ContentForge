# ContentForge — System Specification

**Version:** 1.0  
**Status:** Authoritative Product & Engineering Specification  
**Project Type:** Full-Stack Headless Content Management System  
**Primary Goal:** Demonstrate production-grade C#/.NET and Microsoft Azure engineering capabilities through a complete, cloud-ready content management platform.

This document is the **source of truth for requirements**. Implementation status and as-built architecture are documented in:

- [docs/IMPLEMENTATION_PLAN.md](./docs/IMPLEMENTATION_PLAN.md) — phased delivery (completed)
- [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) — system architecture as implemented
- [README.md](./README.md) — quick start and operations entry points

Do not treat unchecked planning artifacts elsewhere as overrides of this specification. When code and this document conflict, resolve explicitly before changing either.

---

# 1. Project Overview

## 1.1 Product Name

**ContentForge**

## 1.2 Product Description

ContentForge is a production-oriented, headless Content Management System (CMS) designed for creating, managing, reviewing, versioning, publishing, and consuming structured digital content through a RESTful API.

The system consists of:

- a .NET 8 backend;
- a Vue.js administrative frontend;
- a relational database;
- object storage for media;
- authentication and authorization;
- content lifecycle management;
- content versioning;
- audit logging;
- search, filtering, sorting, and pagination;
- API documentation;
- automated testing;
- observability;
- containerized local development;
- CI/CD;
- Microsoft Azure deployment.

ContentForge must be designed as a real production-oriented application rather than as a tutorial project or CRUD demonstration.

The system must maintain clear separation between domain logic, application orchestration, infrastructure concerns, and presentation/API concerns.

---

# 2. Goals

## 2.1 Primary Goals

The application must demonstrate practical competence in:

- C#;
- .NET 8;
- ASP.NET Core;
- Entity Framework Core;
- relational database design;
- REST API design;
- authentication;
- authorization;
- role-based access control;
- domain-driven application design;
- Clean Architecture;
- Vue 3;
- TypeScript;
- state management;
- frontend routing;
- form validation;
- asynchronous workflows;
- file storage;
- Docker;
- GitHub Actions;
- Microsoft Azure;
- cloud deployment;
- application observability;
- automated testing;
- API documentation;
- secure application development.

## 2.2 Secondary Goals

The system should also demonstrate:

- maintainability;
- extensibility;
- testability;
- scalability awareness;
- database migration management;
- structured logging;
- error handling;
- resilient infrastructure integration;
- configuration management;
- API versioning;
- optimistic concurrency;
- production diagnostics.

---

# 3. Non-Goals

ContentForge is not intended to become:

- a WordPress clone;
- a general-purpose website builder;
- a page layout designer;
- a full digital asset management platform;
- a marketing automation platform;
- an enterprise document management system;
- a social network;
- a real-time collaboration suite;
- a SaaS multi-tenant platform.

Features should only be implemented when they support the core CMS use case or meaningfully demonstrate production engineering capabilities.

---

# 4. Target Architecture

ContentForge must use a layered Clean Architecture approach.

```text
┌───────────────────────────────────────────────┐
│                  Vue Frontend                 │
│             Vue 3 + TypeScript               │
└───────────────────────┬───────────────────────┘
                        │ HTTPS / REST
                        ▼
┌───────────────────────────────────────────────┐
│                 ContentForge.Api              │
│        ASP.NET Core Controllers / API         │
│       Authentication / Authorization          │
└───────────────────────┬───────────────────────┘
                        │
                        ▼
┌───────────────────────────────────────────────┐
│            ContentForge.Application            │
│        Use Cases / DTOs / Validators           │
│          Interfaces / Application Logic        │
└───────────────────────┬───────────────────────┘
                        │
                        ▼
┌───────────────────────────────────────────────┐
│              ContentForge.Domain              │
│       Entities / Value Objects / Rules         │
│             Domain Abstractions                │
└───────────────────────────────────────────────┘
                        ▲
                        │
┌───────────────────────┴───────────────────────┐
│          ContentForge.Infrastructure           │
│ EF Core / Database / Identity / Blob Storage  │
│      External Services / Implementations       │
└───────────────────────────────────────────────┘
```

The dependency direction must remain inward.

The Domain layer must not depend on:

- ASP.NET Core;
- Entity Framework Core;
- PostgreSQL;
- Azure SDKs;
- Vue;
- HTTP;
- infrastructure implementations.

---

# 5. Technology Stack

## 5.1 Backend

Required:

- C#;
- .NET 8;
- ASP.NET Core Web API;
- Entity Framework Core;
- ASP.NET Core Identity;
- JWT authentication;
- FluentValidation;
- OpenAPI / Swagger.

Recommended supporting libraries may be introduced when justified, but unnecessary dependencies should be avoided.

## 5.2 Frontend

Required:

- Vue 3;
- TypeScript;
- Vite;
- Vue Router;
- Pinia.

The frontend must use strict TypeScript configuration.

## 5.3 Database

Development:

- PostgreSQL.

Production:

- Azure SQL Database.

Entity Framework Core must abstract database-specific implementation wherever practical.

Database-specific functionality must be isolated.

## 5.4 Storage

Production media storage:

- Azure Blob Storage.

Local development may use:

- local filesystem;
- Azurite;
- or another Azure-compatible development implementation.

The application must access storage through an abstraction.

## 5.5 Infrastructure

Required:

- Docker;
- Docker Compose;
- Microsoft Azure;
- Azure App Service;
- Azure SQL;
- Azure Blob Storage;
- Application Insights.

## 5.6 CI/CD

Required:

- GitHub Actions.

The pipeline must support:

- validation;
- testing;
- building;
- container creation;
- deployment.

---

# 6. Repository Structure

The repository should follow this structure:

```text
/
├── src/
│   ├── ContentForge.Api/
│   ├── ContentForge.Application/
│   ├── ContentForge.Domain/
│   └── ContentForge.Infrastructure/
│
├── frontend/
│   └── contentforge-web/
│
├── tests/
│   ├── ContentForge.UnitTests/
│   ├── ContentForge.IntegrationTests/
│   └── ContentForge.ArchitectureTests/
│
├── docs/
│   ├── architecture/
│   ├── api/
│   ├── decisions/
│   └── operations/
│
├── infra/
│   ├── docker/
│   └── azure/
│
├── .github/
│   └── workflows/
│
├── docker-compose.yml
├── docker-compose.test.yml
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── .gitignore
├── README.md
└── SPECIFICATIONS.md
```

The exact structure may evolve only when a documented architectural reason exists.

---

# 7. Domain Model

The core domain consists of the following concepts.

## 7.1 User

Represents an authenticated CMS user.

Properties:

```text
Id
Email
DisplayName
PasswordHash
IsActive
CreatedAt
UpdatedAt
LastLoginAt
```

The implementation may use ASP.NET Core Identity internally.

---

# 8. Roles and Permissions

The initial roles are:

## Administrator

Full system access.

Permissions include:

- user management;
- role management;
- content type management;
- content management;
- publishing;
- media management;
- audit log access;
- system configuration.

## Editor

Permissions include:

- create content;
- edit content;
- review content;
- publish content;
- manage media.

## Author

Permissions include:

- create content;
- edit own drafts;
- submit content for review;
- manage own content.

## Viewer

Permissions include:

- view administrative content;
- view published content;
- no mutation permissions.

Authorization must be permission-based internally, even if roles are the primary administrative interface.

Example permissions:

```text
content.read
content.create
content.update
content.delete
content.publish
content.archive
content.restore
content.review
content.version.read
content.version.restore

contentType.read
contentType.create
contentType.update
contentType.delete

media.read
media.upload
media.update
media.delete

user.read
user.create
user.update
user.disable

audit.read
```

---

# 9. Content Types

ContentForge must support dynamic content types.

A Content Type defines the structure of a content entry.

Example:

```text
Article

title       → string
slug        → string
excerpt     → text
body        → rich text
coverImage  → media
author      → relation
tags        → relation[]
publishedAt → datetime
```

## 9.1 Content Type Properties

Each content type must contain:

```text
Id
Name
DisplayName
Description
Slug
IsActive
CreatedAt
UpdatedAt
CreatedBy
UpdatedBy
Version
```

## 9.2 Field Definitions

Supported field types:

```text
Text
LongText
RichText
Integer
Decimal
Boolean
Date
DateTime
Media
MediaMultiple
Relation
RelationMultiple
Select
MultiSelect
Json
```

Each field definition must support configuration appropriate to its type.

Examples:

```text
required
defaultValue
minLength
maxLength
minValue
maxValue
pattern
options
relationTarget
allowMultiple
```

## 9.3 Content Type Constraints

The system must enforce:

- unique type names;
- unique slugs;
- valid field names;
- no duplicate fields;
- valid field types;
- valid relation targets;
- safe schema evolution.

Content types must not be deleted when existing content entries depend on them unless an explicit safe deletion workflow is implemented.

---

# 10. Content Entries

A Content Entry represents an instance of a Content Type.

Each entry must have:

```text
Id
ContentTypeId
Slug
Status
Data
CreatedAt
UpdatedAt
CreatedBy
UpdatedBy
PublishedAt
PublishedBy
CurrentVersion
RowVersion
```

Dynamic field data may be stored using JSON where appropriate.

The implementation must maintain a clear distinction between:

- content schema;
- content data;
- system metadata.

---

# 11. Content Lifecycle

Every content entry must support the following states:

```text
DRAFT
    │
    ▼
IN_REVIEW
    │
    ▼
PUBLISHED
    │
    ├──────────────┐
    ▼              │
ARCHIVED           │
                   │
                   └── UNPUBLISHED → DRAFT
```

Valid transitions must be explicitly defined.

Example:

```text
DRAFT → IN_REVIEW
IN_REVIEW → DRAFT
IN_REVIEW → PUBLISHED
PUBLISHED → DRAFT
PUBLISHED → ARCHIVED
ARCHIVED → DRAFT
```

Invalid transitions must return a domain/application error.

Publishing must not be implemented as a simple database status update.

The publish operation must execute all required business rules.

---

# 12. Draft and Published Data

The system must support a distinction between draft content and published content.

Updating a draft must not modify the currently published representation.

When content is published:

1. validation must execute;
2. a version must be created;
3. publication metadata must be recorded;
4. the published representation must become available through the public API;
5. audit information must be generated.

Unpublishing must remove the content from the public API without destroying its historical versions.

---

# 13. Content Versioning

Every meaningful content mutation must be versioned.

A version must contain:

```text
Id
ContentEntryId
VersionNumber
Snapshot
CreatedAt
CreatedBy
ChangeSummary
```

Version numbers must be monotonically increasing per content entry.

The system must support:

- listing versions;
- retrieving a version;
- comparing versions;
- restoring a previous version.

Restoration must create a new version rather than mutating historical data.

Historical versions are immutable.

---

# 14. Optimistic Concurrency

Content editing must support optimistic concurrency.

The system must prevent accidental overwrites when two users edit the same entry simultaneously.

The preferred mechanism is a concurrency token / row version.

Example:

```text
User A loads version 5
User B loads version 5

User A saves → version 6

User B attempts save using version 5
→ HTTP 409 Conflict
```

The API must provide enough information for the frontend to explain the conflict to the user.

---

# 15. Slugs

Content entries exposed through the public API must support human-readable slugs.

Requirements:

- normalized;
- URL-safe;
- unique within a content type;
- validated;
- collision-safe.

Changing a published slug must be treated as a meaningful content change.

---

# 16. Content Validation

Dynamic content must be validated against its Content Type schema.

Validation must support:

- required fields;
- string length;
- numeric ranges;
- regex patterns;
- valid dates;
- enum values;
- relation existence;
- media references;
- JSON structure where configured.

Validation errors must use structured machine-readable responses.

---

# 17. Relations

Content entries must support relationships.

Supported relation types:

```text
one-to-one
many-to-one
one-to-many
many-to-many
```

Examples:

```text
Article → Author
Article → Category
Article → Tags
Product → Category
```

Relations must validate referenced entities.

Deleted or archived referenced entities must be handled according to explicit business rules.

---

# 18. Media Library

ContentForge must provide a media management subsystem.

Supported operations:

```text
upload
list
retrieve metadata
update metadata
delete
```

Media metadata:

```text
Id
FileName
OriginalFileName
ContentType
Size
StorageKey
Url
Width
Height
AltText
Title
Description
UploadedBy
UploadedAt
```

The system must not store large binary files directly in the relational database.

Media binaries must be stored in object storage.

---

# 19. Media Security

Media upload must validate:

- MIME type;
- file extension;
- file size;
- filename;
- storage path.

User-provided filenames must never directly determine storage paths.

The system must prevent path traversal.

Uploaded files must be handled as untrusted input.

---

# 20. Search

The administrative API must support searching content.

Search must support:

- full-text-like keyword matching;
- field filtering;
- status filtering;
- content type filtering;
- author filtering;
- date ranges;
- sorting;
- pagination.

The architecture must keep search implementation replaceable so that a dedicated search engine can be introduced later without rewriting domain logic.

---

# 21. Pagination

Collection endpoints must use consistent pagination.

Preferred parameters:

```text
page
pageSize
```

Maximum page size must be enforced server-side.

Responses must include:

```text
items
page
pageSize
totalItems
totalPages
```

---

# 22. Sorting

Collection endpoints must support controlled sorting.

Clients must only be allowed to sort by explicitly supported fields.

Arbitrary SQL/order expressions must never be accepted from clients.

---

# 23. Filtering

Filtering must use explicit whitelisting.

Example:

```http
GET /api/v1/content/articles?
status=published&
authorId=123&
page=1&
pageSize=20
```

The API must reject unsupported filters rather than silently ignoring them.

---

# 24. Public API

The headless nature of ContentForge requires a dedicated public API.

Example:

```http
GET /api/v1/public/articles
GET /api/v1/public/articles/{slug}

GET /api/v1/public/pages
GET /api/v1/public/pages/{slug}
```

The public API must:

- expose published content only;
- never expose drafts;
- never expose internal audit data;
- never expose user information unless explicitly modeled as public content;
- use stable response DTOs;
- support pagination;
- support filtering where appropriate.

---

# 25. Administrative API

Administrative endpoints must be separated conceptually from public content endpoints.

Examples:

```text
/api/v1/auth/*
/api/v1/users/*
/api/v1/roles/*
/api/v1/content-types/*
/api/v1/content/*
/api/v1/media/*
/api/v1/audit/*
```

---

# 26. API Versioning

All externally exposed APIs must use explicit versioning.

Initial version:

```text
/api/v1/
```

Breaking API changes require a new API version.

---

# 27. REST API Standards

The API must follow conventional HTTP semantics.

Examples:

```text
GET     → retrieve
POST    → create / execute command
PUT     → full replacement
PATCH   → partial update where appropriate
DELETE  → delete
```

Expected status codes include:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
429 Too Many Requests
500 Internal Server Error
```

---

# 28. Error Handling

All API errors must use a consistent structured format.

Preferred format:

```json
{
  "type": "https://contentforge/errors/validation",
  "title": "Validation failed",
  "status": 422,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/content/articles/123",
  "errors": {
    "title": [
      "Title is required."
    ]
  },
  "traceId": "..."
}
```

The implementation should follow RFC 7807 / Problem Details conventions.

Internal exceptions must never leak stack traces or sensitive implementation details in production.

---

# 29. Authentication

Authentication must use:

- ASP.NET Core Identity;
- secure password hashing;
- JWT access tokens.

Requirements:

- secure password policies;
- account activation/deactivation;
- login;
- logout/token invalidation strategy;
- token expiration;
- refresh-token support if implemented;
- secure credential handling.

Passwords must never be logged.

---

# 30. Authorization

Authorization must be enforced server-side.

Frontend restrictions are not security controls.

Every protected mutation endpoint must verify appropriate permissions.

Authorization rules must be centralized and testable.

---

# 31. Account Security

The system must support:

- account lockout or throttling;
- login attempt protection;
- secure password reset workflow;
- email uniqueness;
- disabled account handling.

Sensitive security operations must generate audit events.

---

# 32. Audit Logging

The system must maintain an audit trail for security-sensitive and content-management operations.

Auditable events include:

```text
UserCreated
UserDisabled
UserRoleChanged
LoginSucceeded
LoginFailed
ContentCreated
ContentUpdated
ContentSubmittedForReview
ContentPublished
ContentUnpublished
ContentArchived
ContentRestored
ContentTypeCreated
ContentTypeUpdated
MediaUploaded
MediaDeleted
```

Audit records must include:

```text
Id
Timestamp
UserId
Action
EntityType
EntityId
Metadata
IpAddress
UserAgent
```

Audit entries must be immutable.

---

# 33. Frontend Application

The Vue application is an administrative dashboard.

It must include:

```text
Dashboard
Content
Content Types
Media
Users
Roles
Audit Log
Settings
Profile
```

---

# 34. Frontend Routing

Routes must be protected according to authentication and permissions.

Example:

```text
/login

/dashboard

/content
/content/:contentType
/content/:contentType/new
/content/:contentType/:id
/content/:contentType/:id/edit
/content/:contentType/:id/versions

/content-types
/content-types/new
/content-types/:id/edit

/media

/users
/users/:id

/audit
/settings
/profile
```

Unauthenticated users must be redirected to `/login`.

Unauthorized users must receive a clear access-denied state.

---

# 35. Dynamic Content Editor

The frontend must dynamically render forms based on Content Type definitions.

For example:

```text
Text        → text input
LongText    → textarea
RichText    → rich text editor
Boolean     → checkbox
Integer     → numeric input
Date        → date picker
DateTime    → datetime picker
Select      → select
MultiSelect → multi-select
Media       → media selector
Relation    → relation selector
```

The editor must:

- display validation errors;
- support draft saving;
- display current status;
- expose publishing actions according to permissions;
- display version information;
- handle concurrency conflicts.

---

# 36. Content Editor UX

The editor should clearly display:

```text
Title
Status
Last updated
Last updated by
Current version
Validation state
```

Publishing actions must require explicit user intent.

Destructive actions must require confirmation.

Unsaved changes should be detected where practical.

---

# 37. Dashboard

The dashboard should provide operational information such as:

```text
Total Content
Draft Content
Pending Review
Published Content
Archived Content
Recent Activity
Recent Content
```

Statistics should be retrieved from backend APIs rather than calculated exclusively on the client.

---

# 38. Media UI

The media library should provide:

- grid/list views;
- upload;
- metadata editing;
- search;
- filtering;
- deletion;
- selection from content editors.

Uploads should provide progress feedback where practical.

---

# 39. Frontend State Management

Pinia should be used for shared application state that must survive across routes (for example authentication session, notifications, and global UI loading).

Domain/feature data (content types, entries, media lists, users, audit) may live in route-level views and composables when it does not need to be global. Do not invent Pinia stores solely to mirror every backend resource.

As-built global stores:

```text
authStore
notificationStore
uiStore
```

Transient component state should remain local when global state is unnecessary.

---

# 40. Frontend API Layer

API access must not be scattered across components.

The frontend must have a dedicated API layer.

Example:

```text
api/
├── auth.ts
├── content.ts
├── contentTypes.ts
├── media.ts
├── users.ts
└── audit.ts
```

Authentication headers and common error handling should be centralized.

---

# 41. Accessibility

The frontend must follow reasonable WCAG 2.1 AA practices.

Requirements include:

- keyboard navigation;
- visible focus states;
- semantic HTML;
- accessible labels;
- accessible form errors;
- appropriate contrast;
- non-color-only status indicators.

---

# 42. Responsive Design

The administrative interface must support:

- desktop;
- laptop;
- tablet.

Mobile support is desirable but not the primary target.

---

# 43. Database Design

The database must contain normalized system tables for:

```text
Users
Roles
Permissions
UserRoles
RolePermissions

ContentTypes
ContentTypeFields

ContentEntries
ContentVersions

Media

AuditLogs
```

Additional tables may be introduced for relationships and infrastructure requirements.

---

# 44. EF Core

Entity Framework Core must be used for persistence.

Requirements:

- explicit entity configurations;
- migrations;
- appropriate indexes;
- foreign key constraints;
- concurrency configuration;
- transaction boundaries;
- query optimization.

Entities should not depend on EF Core-specific behavior where avoidable.

---

# 45. Database Migrations

All schema changes must be represented through EF Core migrations.

Migrations must:

- be committed to source control;
- be deterministic;
- be reviewed;
- be tested;
- support deployment automation.

Production schema changes must never rely on manually editing the database.

---

# 46. Database Indexing

Indexes must be created for frequently queried fields.

Expected examples:

```text
ContentEntry.ContentTypeId
ContentEntry.Status
ContentEntry.Slug
ContentEntry.CreatedAt
ContentEntry.UpdatedAt
ContentVersion.ContentEntryId
AuditLog.Timestamp
AuditLog.UserId
```

Indexes must be based on actual query patterns.

---

# 47. Transactions

Operations requiring atomicity must execute within explicit transaction boundaries.

Examples:

```text
publish content
restore version
delete content
change content type schema
```

Transactions must not unnecessarily span external network calls.

---

# 48. Caching

The architecture should support caching of frequently requested public content.

Caching must be introduced only where beneficial.

Potential implementation:

- ASP.NET Core output/data caching;
- distributed cache;
- Azure-compatible cache in production.

Cache invalidation must occur when published content changes.

---

# 49. Rate Limiting

Public and authentication endpoints must use rate limiting.

At minimum:

```text
login
password reset
public content API
media upload
```

must have appropriate limits.

Rate limits must be configurable.

---

# 50. Security Requirements

The system must follow secure-by-default principles.

Requirements include:

- HTTPS in production;
- secure authentication;
- authorization on every protected operation;
- input validation;
- output encoding;
- SQL injection protection through EF Core;
- safe file handling;
- rate limiting;
- secret management;
- secure headers;
- CORS restrictions;
- no sensitive information in logs;
- no secrets in source control.

---

# 51. CORS

Production CORS configuration must use an explicit allowlist.

Wildcard origins must not be used in production.

Allowed origins must come from configuration.

---

# 52. Secrets

Secrets must never be committed to Git.

Local development may use:

```text
.NET User Secrets
.env
```

Production secrets should use appropriate Azure secret-management facilities.

---

# 53. Configuration

Configuration must be environment-aware.

Supported environments:

```text
Development
Test
Staging
Production
```

Environment-specific behavior must be configuration-driven rather than implemented through hardcoded conditionals.

---

# 54. Logging

Application logging must use structured logging.

Logs should contain:

```text
timestamp
level
message
service
environment
traceId
correlationId
userId where appropriate
```

Logs must not contain:

- passwords;
- access tokens;
- refresh tokens;
- sensitive personal data;
- secrets.

---

# 55. Observability

Production deployment must integrate with Azure Application Insights.

The application must expose:

- request telemetry;
- exception telemetry;
- dependency telemetry;
- performance metrics;
- structured logs.

Health endpoints must be provided.

Example:

```text
/health/live
/health/ready
```

---

# 56. Health Checks

Liveness checks must determine whether the process is running.

Readiness checks must verify required dependencies such as:

- database;
- required storage infrastructure.

Health endpoints must not expose secrets or detailed internal information.

---

# 57. Correlation IDs

Requests must have a correlation/trace identifier.

The identifier should:

- be accepted from trusted incoming headers where appropriate;
- otherwise be generated;
- appear in logs;
- be returned in error responses.

---

# 58. API Documentation

OpenAPI documentation is mandatory.

The API documentation must include:

- endpoints;
- request models;
- response models;
- authentication;
- validation errors;
- status codes;
- examples where useful.

Swagger UI may be enabled in development and controlled environments.

---

# 59. Testing Strategy

Testing is a first-class requirement.

The project must include:

```text
Unit Tests
Integration Tests
Architecture Tests
Frontend Unit Tests
Frontend Component Tests
End-to-End Tests
```

---

# 60. Unit Tests

Unit tests must cover:

- domain rules;
- content lifecycle;
- validation;
- permission logic;
- slug generation;
- versioning;
- publishing rules;
- concurrency behavior;
- application use cases.

Tests must be deterministic.

---

# 61. Integration Tests

Integration tests must verify:

- HTTP endpoints;
- authentication;
- authorization;
- EF Core persistence;
- migrations;
- database constraints;
- transactions;
- API error responses.

The test environment should use a real relational database where practical.

---

# 62. Architecture Tests

Architecture tests must ensure:

- Domain does not depend on Infrastructure;
- Domain does not depend on API;
- Application does not depend on API;
- Infrastructure dependencies remain isolated;
- forbidden dependency directions are rejected.

---

# 63. Frontend Testing

Frontend tests must cover:

- stores;
- API integration behavior;
- dynamic form rendering;
- validation;
- route guards;
- permission-based UI;
- critical components.

---

# 64. End-to-End Tests

Critical user journeys must be tested end-to-end.

Required scenarios:

### Authentication

```text
login
logout
invalid credentials
disabled account
```

### Content

```text
create content
edit content
submit for review
publish
unpublish
archive
restore version
```

### Authorization

```text
author cannot publish
viewer cannot edit
editor can publish
administrator can manage users
```

### Media

```text
upload
select media
delete media
```

---

# 65. Test Data

Tests must use isolated data.

Tests must not depend on:

- production data;
- developer machines;
- execution order;
- external mutable services.

Factories/builders should be used for complex domain test objects.

---

# 66. Code Quality

The project must enforce:

- nullable reference types;
- analyzers;
- formatting;
- compiler warnings treated seriously;
- consistent naming;
- XML documentation where useful;
- no dead code;
- no commented-out production code;
- no unexplained magic values.

---

# 67. Dependency Management

Dependencies must be:

- justified;
- actively maintained;
- kept up to date;
- pinned through appropriate package management mechanisms.

Unused dependencies must be removed.

---

# 68. Docker

The project must provide Docker support.

Development should be possible using:

```bash
docker compose up
```

The environment should provide required infrastructure such as:

```text
PostgreSQL
Azurite where needed
```

The application itself should have a production-oriented Dockerfile where appropriate.

---

# 69. Container Requirements

Production containers should:

- run as non-root where practical;
- use multi-stage builds;
- minimize final image size;
- avoid unnecessary packages;
- expose only required ports;
- receive configuration through environment variables.

---

# 70. CI Pipeline

GitHub Actions must execute at minimum:

```text
restore
build
format/lint validation
unit tests
integration tests
architecture tests
frontend tests
frontend build
backend publish
```

A pull request must not be considered mergeable when required checks fail.

---

# 71. CD Pipeline

Deployment pipeline:

```text
commit
   ↓
CI validation
   ↓
build artifacts
   ↓
container image
   ↓
deployment
   ↓
health check
   ↓
deployment verification
```

Production deployment must require appropriate protection/approval mechanisms.

---

# 72. Azure Architecture

Target production architecture:

```text
                    Internet
                       │
                       ▼
               Azure App Service
                       │
             ┌─────────┴─────────┐
             │                   │
             ▼                   ▼
        ASP.NET Core        Vue Frontend
             │
      ┌──────┼───────────┐
      │      │           │
      ▼      ▼           ▼
 Azure SQL  Blob     Application
 Database   Storage   Insights
```

The exact hosting topology may evolve based on deployment requirements.

---

# 73. Azure App Service

The backend must be deployable to Azure App Service.

Requirements:

- HTTPS;
- environment variables;
- health checks;
- logging;
- deployment slots where appropriate;
- managed identity where supported.

---

# 74. Azure SQL

Production relational persistence must use Azure SQL Database.

Requirements:

- secure connectivity;
- firewall/network restrictions;
- encrypted connections;
- migration strategy;
- backup/recovery configuration;
- monitoring.

---

# 75. Azure Blob Storage

Media files must use Azure Blob Storage in production.

The application must interact through an abstraction such as:

```text
IFileStorage
```

This allows local and production implementations to differ without changing domain/application code.

---

# 76. Azure Identity

Where supported, managed identity should be preferred over long-lived credentials for Azure service-to-service access.

Secrets should not be embedded in:

- source code;
- Docker images;
- GitHub workflow files.

---

# 77. Deployment Environments

The project should support:

```text
Development
Staging
Production
```

Staging should resemble production sufficiently to validate deployments.

---

# 78. Database Deployment

Database migrations must be handled explicitly during deployment.

The deployment strategy must avoid uncontrolled automatic production schema changes.

A migration process must be documented.

---

# 79. Backups and Recovery

Production database configuration must support:

- automated backups;
- point-in-time recovery where available;
- documented recovery procedure.

Media storage must use appropriate redundancy and retention configuration.

---

# 80. Disaster Recovery

The documentation must describe:

- database recovery;
- media recovery;
- application redeployment;
- configuration recovery;
- secret recovery;
- restoration verification.

---

# 81. Performance Requirements

The system should be designed for:

- efficient pagination;
- asynchronous I/O;
- indexed queries;
- avoiding N+1 queries;
- bounded result sets;
- cancellation token propagation;
- efficient serialization.

Database access must not occur synchronously on request threads.

---

# 82. Scalability

The backend should remain stateless where practical.

Application instances must not depend on local process memory for durable application state.

The system should be horizontally scalable at the application layer.

---

# 83. Cancellation

HTTP request cancellation tokens must be propagated to:

- database queries;
- storage operations;
- long-running application operations.

---

# 84. API Idempotency

Operations where duplicate requests could create undesirable results should be designed to be idempotent where practical.

Publishing and deletion operations should be safe against reasonable client retries.

---

# 85. Background Processing

Long-running or asynchronous operations should not block HTTP requests.

The architecture should provide an abstraction for background jobs.

Potential future operations include:

- media processing;
- search indexing;
- cleanup;
- scheduled publishing.

A full distributed job-processing system is not required unless justified.

---

# 86. Scheduled Publishing

The final production target should support scheduled publishing.

A content entry may specify:

```text
publishAt
unpublishAt
```

The system must publish/unpublish content automatically at the configured time.

The scheduling mechanism must be resilient against:

- duplicate execution;
- process restarts;
- delayed execution.

---

# 87. Content Preview

Editors must be able to preview draft content before publication.

Preview access must be authenticated and must not expose drafts through the public API.

Preview URLs/tokens must be:

- short-lived;
- scoped;
- revocable where practical.

---

# 88. Content Type Schema Evolution

Changing a Content Type must be handled carefully.

Examples:

```text
add field
remove field
rename field
change validation
change relation
```

The system must prevent schema changes from silently corrupting existing content.

Potentially destructive changes must require explicit confirmation.

---

# 89. Soft Deletion

Where appropriate, business entities should support soft deletion.

Soft-deleted content must:

- disappear from normal queries;
- remain recoverable;
- remain available for audit/history rules.

Permanent deletion should be restricted.

---

# 90. Auditability

Important state transitions must always be explainable through:

- audit records;
- version history;
- timestamps;
- user identity.

The system should make it possible to answer:

> Who changed this content, what changed, and when?

---

# 91. API Security Testing

Security-related integration tests must cover:

- unauthorized access;
- privilege escalation attempts;
- invalid JWTs;
- expired credentials;
- disabled users;
- forbidden operations;
- malformed input;
- excessive payloads;
- invalid file uploads.

---

# 92. Dependency Security

CI should include dependency vulnerability scanning where practical.

Critical vulnerabilities must block production release unless explicitly reviewed and accepted.

---

# 93. Frontend Security

The frontend must:

- avoid storing sensitive secrets in source;
- avoid exposing backend credentials;
- handle authentication securely;
- avoid rendering untrusted HTML without sanitization;
- avoid unsafe dynamic code execution.

Rich text rendering must sanitize untrusted HTML.

---

# 94. Accessibility and UX Quality Gates

Before production release:

- no critical accessibility violations;
- no broken navigation;
- no unhandled form errors;
- no console errors in normal workflows;
- loading states must exist for asynchronous operations;
- error states must be understandable.

---

# 95. Documentation

The repository must contain:

```text
README.md
SPECIFICATIONS.md
docs/architecture/
docs/api/
docs/decisions/
docs/operations/
```

Architecture decisions should be documented as ADRs.

---

# 96. Architecture Decision Records

Important decisions should be recorded.

Examples:

```text
ADR-001 Clean Architecture
ADR-002 PostgreSQL locally / Azure SQL in production
ADR-003 Dynamic content schema representation
ADR-004 Authentication strategy
ADR-005 Media storage abstraction
ADR-006 Content versioning strategy
ADR-007 Publishing architecture
```

---

# 97. Developer Experience

A new developer must be able to:

1. clone the repository;
2. configure local environment;
3. start dependencies;
4. run backend;
5. run frontend;
6. execute tests;
7. access Swagger;
8. access the frontend.

The README must document the complete process.

---

# 98. Local Development

The project should support a predictable developer workflow.

Example:

```bash
docker compose up -d
dotnet restore
dotnet build
dotnet test
npm install
npm run dev
```

Exact commands may differ based on implementation.

---

# 99. Seed Data

Development mode should provide optional deterministic seed data.

Seed data should include:

- administrator;
- editor;
- author;
- viewer;
- sample content types;
- sample content;
- sample media metadata;
- audit events.

Production must never use development seed credentials.

---

# 100. Demo Content Types

The default demo environment should include:

### Article

```text
title
slug
excerpt
body
coverImage
author
tags
publishedAt
```

### Page

```text
title
slug
body
seoTitle
seoDescription
```

### Author

```text
name
bio
avatar
```

### Category

```text
name
slug
description
```

These are examples and must be represented using the dynamic Content Type system rather than hardcoded domain entities where practical.

---

# 101. SEO Metadata

Content types may support SEO-oriented fields such as:

```text
seoTitle
seoDescription
canonicalUrl
robots
```

The CMS must treat these as content fields rather than tightly coupling them to a specific frontend implementation.

---

# 102. API Consumer Experience

The public API must be usable by an external application without requiring knowledge of the administrative frontend.

API responses must be stable and documented.

The repository should contain example API usage.

---

# 103. API Client Generation

The OpenAPI specification should be suitable for generating typed API clients.

The frontend may use generated TypeScript types where this improves consistency.

The generated API contract must not become a replacement for backend domain modeling.

---

# 104. Error and Validation Consistency

Validation behavior must be consistent across:

```text
Domain
Application
API
Frontend
```

The backend remains the authoritative validation layer.

Frontend validation is a usability feature, not a security boundary.

---

# 105. Production Readiness

The application is considered production-ready only when all of the following are true:

```text
[ ] Build passes
[ ] All automated tests pass
[ ] Architecture tests pass
[ ] Database migrations are valid
[ ] API contract is documented
[ ] Authentication is implemented
[ ] Authorization is implemented
[ ] Audit logging works
[ ] Error handling is consistent
[ ] Health checks work
[ ] Structured logging works
[ ] Application Insights integration works
[ ] Secrets are externalized
[ ] Docker image builds
[ ] CI pipeline passes
[ ] Deployment pipeline passes
[ ] Azure deployment succeeds
[ ] Database backup strategy is documented
[ ] Recovery procedure is documented
[ ] Security checks pass
[ ] Frontend production build succeeds
[ ] Critical E2E scenarios pass
[ ] README is complete
```

---

# 106. Definition of Done

A feature is considered complete only when:

1. requirements are implemented;
2. domain/application behavior is tested;
3. API behavior is tested;
4. authorization is tested;
5. relevant frontend behavior is tested;
6. error cases are handled;
7. logging is appropriate;
8. documentation is updated;
9. migrations are included where necessary;
10. code formatting passes;
11. static analysis passes;
12. no unrelated regressions exist.

---

# 107. AI-First Development Rules

ContentForge will be developed primarily using AI coding agents such as Cursor and Claude Code.

The repository must therefore be optimized for machine-assisted development.

## 107.1 Source of Truth

`SPECIFICATIONS.md` is the authoritative product and engineering specification.

AI agents must not silently redefine requirements.

When implementation conflicts with this specification:

1. identify the conflict;
2. explain it;
3. propose a solution;
4. wait for explicit approval when the change is architectural or breaking.

---

# 108. AI Agent Operating Principles

AI agents must:

- inspect existing code before modifying it;
- understand dependency boundaries;
- reuse existing abstractions;
- avoid unnecessary rewrites;
- preserve working functionality;
- implement tests with behavior changes;
- run relevant validation commands;
- report failures honestly;
- avoid speculative features;
- avoid introducing unnecessary dependencies.

Agents must not claim that a task is complete without verifying it.

---

# 109. Incremental Development

Implementation must proceed in small, logically isolated increments.

Each increment should:

1. have a clear objective;
2. modify the minimum required files;
3. include tests;
4. run relevant checks;
5. preserve previous functionality.

Large uncontrolled rewrites are prohibited unless explicitly requested.

---

# 110. AI Code Review Requirements

After every major subsystem, an AI-driven review must verify:

- architecture;
- security;
- correctness;
- test coverage;
- performance;
- maintainability;
- specification compliance.

Reviews must identify concrete findings rather than merely stating that code looks good.

---

# 111. Required Verification

Before declaring any implementation phase complete, the agent must run appropriate checks.

Backend:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Frontend:

```bash
npm run build
npm run test
npm run lint
```

Exact commands may evolve with the project tooling.

---

# 112. No Fake Completion

AI agents must never:

- skip tests without reporting it;
- replace failing tests with weaker tests;
- disable analyzers to make CI pass;
- suppress warnings without justification;
- fake external service responses as production implementations;
- leave TODO placeholders for required functionality;
- claim Azure deployment without verifying deployment;
- claim security compliance without testing.

---

# 113. Git Strategy

Commits should be:

- small;
- logically grouped;
- descriptive;
- buildable where practical.

Preferred format:

```text
feat(content): add content lifecycle management
feat(auth): implement JWT authentication
feat(media): add Azure blob storage
test(content): add publishing integration tests
refactor(api): extract validation pipeline
docs(architecture): document persistence strategy
```

---

# 114. Pull Request Quality

Every significant change should be reviewable as an independent unit.

Pull requests should contain:

```text
Summary
Changes
Tests
Architecture impact
Security impact
Migration impact
Deployment impact
```

---

# 115. Release Strategy

The application should use semantic versioning:

```text
MAJOR.MINOR.PATCH
```

Breaking API changes require a major version or explicit API versioning strategy.

---

# 116. Final Product Capability Matrix

The completed ContentForge platform should support:

| Capability | Required |
|---|---:|
| User authentication | Yes |
| RBAC | Yes |
| Permissions | Yes |
| Dynamic content types | Yes |
| Dynamic fields | Yes |
| Content CRUD | Yes |
| Drafts | Yes |
| Review workflow | Yes |
| Publishing | Yes |
| Unpublishing | Yes |
| Archiving | Yes |
| Version history | Yes |
| Version restore | Yes |
| Optimistic concurrency | Yes |
| Relations | Yes |
| Media library | Yes |
| Azure Blob Storage | Yes |
| Search | Yes |
| Filtering | Yes |
| Sorting | Yes |
| Pagination | Yes |
| Public API | Yes |
| Admin API | Yes |
| API versioning | Yes |
| OpenAPI | Yes |
| Audit log | Yes |
| Rate limiting | Yes |
| Health checks | Yes |
| Structured logging | Yes |
| Application Insights | Yes |
| Docker | Yes |
| GitHub Actions | Yes |
| Azure App Service | Yes |
| Azure SQL | Yes |
| Database migrations | Yes |
| Unit tests | Yes |
| Integration tests | Yes |
| Architecture tests | Yes |
| E2E tests | Yes |
| Frontend tests | Yes |
| CI/CD | Yes |
| Scheduled publishing | Yes |
| Draft preview | Yes |
| Content schema evolution | Yes |
| Soft deletion | Yes |
| Backup/recovery documentation | Yes |
| ADR documentation | Yes |

---

# 117. Portfolio Positioning

ContentForge should demonstrate the ability to design and implement a complete modern enterprise-style application rather than only individual technical features.

The project should communicate the following engineering capabilities:

> **Backend engineering**

C#, ASP.NET Core, EF Core, REST APIs, domain modeling, validation, security, testing.

> **Frontend engineering**

Vue 3, TypeScript, state management, dynamic forms, API integration, accessibility.

> **Database engineering**

Relational modeling, migrations, indexing, transactions, concurrency, SQL.

> **Cloud engineering**

Azure App Service, Azure SQL, Azure Blob Storage, managed identity, monitoring.

> **DevOps**

Docker, GitHub Actions, CI/CD, deployment verification, environment management.

> **Production engineering**

Authentication, authorization, auditing, observability, resilience, security, testing, documentation.

---

# 118. Final Architectural Principle

ContentForge must remain a coherent application rather than a collection of technology demonstrations.

Every technology must exist because it solves a real architectural or product requirement.

The project should favor:

```text
clarity
correctness
testability
security
maintainability
observability
```

over unnecessary complexity.

The final result should be credible as a production-oriented enterprise application and sufficiently complete to serve as a strong portfolio demonstration of modern **C#/.NET + Vue + SQL + Azure** engineering.