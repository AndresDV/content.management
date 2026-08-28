# Content Management

A .NET 9 service that ingests CMS lifecycle events (`publish` / `unpublish` /
`delete`) via a webhook, tracks versioned entities with explicit publication
state, and exposes them through a read-only REST API with a local admin disable
override. Built with Clean Architecture, CQRS, EF Core + PostgreSQL, MediatR,
Serilog, and xUnit.

## Core principle

The database represents the latest valid CMS state, while distinguishing:

1. the **latest CMS version** (tracked per entity),
2. the **published state** (what normal users see),
3. the **local admin override** (`IsDisabled`) — an API-side flag that never
   affects CMS data.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)

## Setup

1. Start PostgreSQL:

   ```bash
   docker compose -f infrastructure-docker-compose.yml up -d
   ```

2. Run the API:

   ```bash
   dotnet run --project Content.Management.Api
   ```

   EF Core migrations are applied automatically on startup in the
   `Development` environment. Swagger UI is available at
   `http://localhost:5000/swagger`.

To create a new migration:

```bash
dotnet ef migrations add <Name> --project Content.Management.Infrastructure
```

## Configuration

| Setting | Default (Development) | Description |
|---|---|---|
| `ConnectionStrings:ContentManagement` | `Host=localhost;Port=5432;Database=contentmanagement;Username=contentmgmt;Password=ContentMgmtLocal` | PostgreSQL connection string (override via `ConnectionStrings__ContentManagement`) |
| `Authentication:Organization` | see below | CMS webhook Basic-auth credentials |
| `Authentication:Users:ApiUsers` | see below | API consumer credentials + roles |

### Environments

Configuration is layered via one `appsettings` file per environment:

- `appsettings.json` — shared defaults (logging, hosts).
- `appsettings.Development.json` — local development (connection string + dev credentials).
- `appsettings.Staging.json` — staging (logging only; secrets from environment variables).
- `appsettings.Production.json` — production (logging only; secrets from environment variables).

In **Staging** and **Production** the connection string and credentials are not
committed — they are injected at deploy time via environment variables
(`ConnectionStrings__ContentManagement`, `Authentication__Organization__Username`,
`Authentication__Organization__Password`, `Authentication__Users__ApiUsers__0__*`,
…). The application fails fast at startup if the connection string is missing.

## Authentication

Two independent Basic-auth boundaries:

| Boundary | Audience | Default credentials (Development) |
|---|---|---|
| Organization | CMS → webhook (`POST …/events`) | username `content-integration`, password `9e303e1b-66cb-443f-a889-780beb327d51` |
| Users | API consumers | `content.consumer@lateralgroup.com` / `Consumer#2024!` (User) and `content.admin@lateralgroup.com` / `Admin#2024!` (Admin) |

Development credentials are provided in `appsettings.Development.json` and can be
overridden via environment variables. An admin is simply a configured user with
`Role: "Admin"`. For Staging/Production, provide real credentials via the pipeline
secrets/variables (never committed).


## API

| Operation | Endpoint | Auth |
|---|---|---|
| Ingest batch | `POST /api/content-management/events` | Organization |
| List entities | `GET /api/content-management/entities` | Users |
| Get entity | `GET /api/content-management/entities/{id}` | Users |
| Disable entity | `POST /api/content-management/entities/{id}/disable` | Users (Admin) |

Consumers **cannot** create, update, or delete data — the CMS is the source of
truth. Visibility: normal users see `IsPublished && !IsDisabled`; admins see
everything.

### HTTP status codes

| Endpoint | Status | Meaning |
|---|---|---|
| `POST …/events` | `200 OK` | batch ingested and processed synchronously (body `{ "processed": N }`) |
| `POST …/events` | `400 Bad Request` | one or more events failed validation |
| `POST …/events` | `401 Unauthorized` | missing/invalid organization credentials |
| `GET …/entities` | `200 OK` | list of entities visible to the caller |
| `GET …/entities` | `401 Unauthorized` | missing/invalid user credentials |
| `GET …/entities/{id}` | `200 OK` | entity found and visible |
| `GET …/entities/{id}` | `404 Not Found` | entity absent, or not visible to the caller's role |
| `POST …/entities/{id}/disable` | `204 No Content` | entity disabled (local override) |
| `POST …/entities/{id}/disable` | `403 Forbidden` | authenticated but not an admin |
| `POST …/entities/{id}/disable` | `404 Not Found` | entity absent |

> The webhook returns `200 OK` (not `202 Accepted`) because events are processed
> synchronously — the response is only sent after the batch is persisted. `202`
> would imply asynchronous, later processing.

The webhook accepts a batch of events:

```json
[
  { "type": "publish",   "id": "X", "payload": { "name": "My Event" }, "version": 2 },
  { "type": "delete",    "id": "Y" },
  { "type": "unpublish", "id": "Z", "payload": { "name": "Other" }, "version": 4 }
]
```

## Manual testing

Ready-to-import Postman files are provided under `postman/`:

- `Content.Management.postman_collection.json` — every endpoint with Basic auth + response assertions, plus a **Scenarios** folder of ordered, self-verifying sequences (publish, multi-version, unpublish, the never-published corner case, delete, out-of-order, admin disable, batch).
- `Content.Management.postman_environment.json` — the `baseUrl`, entity id, and credential variables.

In Postman, **Import** both files, then select the environment
(**Content Management - Local**) as active. Requests resolve their URLs and
credentials from the environment variables — edit those values if your setup
differs. Run the collection via the **Runner** (or run requests individually); run
the **Scenarios** folder top-to-bottom to exercise each business rule end to end.

In **Swagger UI**, click **Authorize** and enter a username/password (organization
credentials for the events endpoint, user/admin credentials for the entities
endpoints).

With **Postman** manually, use the *Authorization* tab → *Basic Auth* and enter the
username/password (Postman encodes it for you).

With **curl** (`-u` encodes Basic auth automatically):

```bash
# Ingest events — organization credentials
curl -X POST http://localhost:5000/api/content-management/events \
  -u content-integration:9e303e1b-66cb-443f-a889-780beb327d51 \
  -H "Content-Type: application/json" \
  -d '[{"type":"publish","id":"entity-1","payload":{"name":"My Event"},"version":1}]'

# List entities — user credentials
curl http://localhost:5000/api/content-management/entities \
  -u content.consumer@lateralgroup.com:'Consumer#2024!'

# Get one entity — user credentials
curl http://localhost:5000/api/content-management/entities/entity-1 \
  -u content.consumer@lateralgroup.com:'Consumer#2024!'

# Disable an entity — admin credentials
curl -X POST http://localhost:5000/api/content-management/entities/entity-1/disable \
  -u content.admin@lateralgroup.com:'Admin#2024!'
```

## Design decisions

- **Synchronous processing** — events are validated and processed on the request
  thread (async/await for I/O, no background queue). Chosen because correctness
  is the priority; it avoids the retry/durability/eventual-consistency complexity
  of an async pipeline.
- **PostgreSQL via Docker** — a production-oriented relational DB that works on
  both macOS and Windows.
- **Two auth boundaries** — the CMS organization and API consumers use separate,
  independent credentials.
- **Admin visibility** — admins have complete visibility (published +
  unpublished + disabled).
- **No version history** — only the latest version is tracked, per the
  requirement; publication state is tracked separately.

## Future production considerations

The following were consciously **not implemented** for this assessment (out of the
stated requirements or unnecessary at this scale), but are expected considerations
for a production release. They are documented here to preserve the reasoning and
surface the known trade-offs.

### Authentication & authorization

The current implementation uses **HTTP Basic authentication** with two
configuration-driven user pools and a simple role claim. This is intentionally
minimal for the assessment. In production, this would be replaced or augmented with:

- **JWT bearer tokens** (short-lived access tokens + refresh tokens) issued by an
  identity provider, rather than credentials on every request.
- **OAuth 2.0 / OpenID Connect** with a managed provider (e.g., **Microsoft Entra ID**,
  Auth0, Okta) for SSO, MFA, and centralized user/role management.
- **Scoped authorization policies / RBAC** and per-resource authorization beyond the
  current `User`/`Admin` split.

The Basic-auth handlers and the `UserRole`/policy enumerations are isolated behind
`AddSecurity`, so the scheme can be swapped without touching the domain/application
layers.

### Caching (read path)

Read endpoints currently hit PostgreSQL directly (`AsNoTracking`). For higher read
load, a distributed cache would be introduced for the query path — e.g., **Redis**
(optionally with a sidecar like Redis Stack) — with:

- Cache-aside invalidation keyed by entity id, invalidated on
  `publish`/`unpublish`/`delete`/`disable` events.
- TTL-based staleness for list endpoints, or query-cache via an abstraction
  (`IContentManagementEntityQueries`) so a cached decorator can wrap the existing
  implementation without changing consumers.

### Performance & scalability

No database-side tuning has been applied yet because current data volumes don't
justify it. For production the following would be evaluated and measured (with load
tests) before committing to them:

- **Database indexes** on hot query columns (e.g., `IsPublished`, `IsDisabled`,
  `Version`) and composite indexes for the visibility filter.
- **Connection pooling / `Npgsql` tuning** (max pool size, command timeouts).
- **Query optimization** — `EXPLAIN ANALYZE`, `QuerySplittingBehavior`, compiled
  queries, and pagination (keyset/cursor) for large result sets.
- **Throughput of EF Core** under realistic write/read concurrency — including
  optimistic-concurrency (`xmin`/rowversion) to guard concurrent updates of the
  same entity (currently the in-memory version guard handles out-of-order
  *sequencing*, not simultaneous writes).
- Horizontal scaling of the stateless API behind a load balancer, with the database
  as the coordination point.

### Infrastructure & deployment

The repository ships a **containerization + CI/CD baseline** (`.github/workflows`,
`azure-pipelines.yml`, `deploy/*.sh`) so the service can be packaged and released,
but the target topology is left open:

- **Cloud vs. on-premise** — the pipelines assume an Azure Container Apps + Azure
  Container Registry shape, but any registry/orchestrator (Kubernetes, App Service,
  on-prem Docker) would work with minor changes.
- **Secrets management** — credentials/connection strings are injected via
  environment variables today; a secret store (Key Vault, Vault, k8s secrets) is
  the production expectation.
- **Observability beyond logs** — structured Serilog is present; production would
  add OpenTelemetry traces/metrics, distributed tracing, and alerting.
- **Database migrations in higher environments** — migrations auto-apply only in
  Development; a dedicated migration job (or `dotnet ef database update` step)
  would be added to the deployment pipeline for Staging/Production.

## Observability

Serilog logs every event with structured fields `Type`, `EntityId`, `Version`,
and `Status` (`received` / `rejected` / `processed` / `failed`), including
failures.

## Deployment

CI/CD is provided for three environments (Development, Staging, Production):

- `.github/workflows/ci.yml` — builds the solution and runs unit + integration
  tests on push/PR to `main`/`develop`.
- `.github/workflows/deploy.yml` — manually triggered, builds and pushes the Docker
  image to Azure Container Registry, then deploys to the selected environment
  (Development/Staging/Production) via the corresponding `deploy/*.sh` script.
- `azure-pipelines.yml` — equivalent Azure DevOps pipeline (parameterized by
  environment).

Secrets (registry credentials, Azure OIDC) are stored as GitHub/Azure secrets or
variables; environment-specific connection strings and credentials are injected at
deploy time via environment variables and are never committed.

## Tests

```bash
dotnet test
```

Unit tests cover the domain rules (publish/unpublish/delete, the unpublish corner
case, out-of-order version guard, visibility). Integration tests spin up a
PostgreSQL container via Testcontainers (requires Docker) and exercise the
webhook → query flow, authentication, visibility, and admin disable.

## Tech Stack

.NET 9 · C# 13 · ASP.NET Core · EF Core (Npgsql) · PostgreSQL · MediatR 12.5.0 ·
FluentValidation · Serilog · Swashbuckle · xUnit · NSubstitute · AutoFixture ·
FluentAssertions · Testcontainers

## Project Structure

```
content.management.sln
├── src/
│   ├── Content.Management.Domain
│   ├── Content.Management.Application
│   ├── Content.Management.Infrastructure
│   └── Content.Management.Api
└── tests/
    ├── Content.Management.UnitTests
    └── Content.Management.IntegrationTests
```
