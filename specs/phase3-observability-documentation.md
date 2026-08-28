# Phase 3 — Observability, Documentation, Polish

- **Date:** 2026-08-27
- **Status:** Approved
- **Scope:** structured event logging, README, architecture docs, final acceptance

## 1. Goal

Add structured observability for event processing, complete the README and
architecture documentation, and verify the deliverable end-to-end.

## 2. Observability

Serilog structured logging uses centralized `LoggerExtensions` helpers — `Debug`
for lifecycle logs and `Error` for validation errors and failures. (OpenTelemetry
and gRPC-specific telemetry are intentionally not used.)

### Event processing logs

Every CMS event is logged, capturing:

- `type` (`publish` / `unpublish` / `delete`)
- `entityId`
- `version`
- `status` (`received` / `processed` / `rejected` / `failed`)
- `error` (on failure)

Logs are emitted at:
- **received** — webhook accepts the batch/event.
- **rejected** — validation/sanitization failure.
- **processed** — successful command handling.
- **failed** — exception during processing.

Implemented via `LoggerExtensions`:
- `LogEventReceived(type, entityId, version)`
- `LogEventRejected(type, entityId, version, reason)`
- `LogEventProcessed(type, entityId, version)`
- `LogEventFailed(type, entityId, version)` and `LogEventFailed(..., exception)`

Each logs `Type`, `EntityId`, `Version`, and `Status` (plus `Reason`/`Error`).
The ingestion flow (`ContentManagementEntityQueries.IngestContentEventsAsync`) emits
received/rejected/failed; the `Publish`/`Unpublish`/`Delete` handlers emit
processed/failed.

### Command & domain-event logs

Command and domain-event lifecycle logs use `LoggerExtensions`:

- `LogCommandHandlingStarted(command)` — `Debug`, "Handling command".
- `LogCommandValidationErrors(command, errors)` — `Error`.
- `LogCommandSent(command)` — `Debug`.
- `LogDomainEventHandlingStarted(@event)` — `Debug`.
- `LogDomainValidationErrors(@event, errors)` — `Error`.

The `LoggingBehaviour` pipeline behavior logs only on exception
(`Error handling the command: {CommandName}`).

HTTP requests are additionally logged via `UseSerilogRequestLogging` (method, path,
status code, duration). Logs are written to the console sink, configured per
environment in `appsettings*.json` (`Serilog:WriteTo: Console`).

## 3. Documentation

- **README.md**:
  - Overview and the core principle (latest version vs published state vs admin override).
  - Setup/run (Docker Compose for PostgreSQL + `dotnet run`).
  - Endpoint table.
  - Documented decisions:
    - **Synchronous processing** (correctness over scale; async/await for I/O, no background queue) — per the assessment's "understand why".
    - **Admin visibility** interpretation (admin sees everything).
    - **PostgreSQL via Docker** (production-oriented relational DB, cross-platform).
    - **Two auth boundaries** (organization vs users).
  - Configuration reference (connection string, auth credentials).
  - Test instructions.
  - Deployment reference (CI/CD workflows + per-environment `appsettings`).
- **Architecture docs** (`docs/architecture/*.md`, mermaid): align to the final
  implementation (system context, domain aggregate, sequence diagrams).
- **Swagger**: Basic-auth security definition so the **Authorize** button is
  available for testing endpoints; document manual testing via Swagger, Postman
  (Basic Auth tab), and curl (`-u`).
- **Postman** (under `postman/`):
  - `Content.Management.postman_collection.json` — every endpoint (webhook
    publish/unpublish/delete + invalid/auth-failure, read as user vs admin, admin
    disable) with per-request Basic auth and response assertions, plus a
    **Scenarios** folder of ordered, self-verifying request sequences covering the
    business rules: publish→visible, multi-version (latest wins), unpublish,
    the **corner case** (unpublish of a never-published version), delete,
    out-of-order versions (highest wins), admin disable, and batch ingestion.
  - `Content.Management.postman_environment.json` — the `baseUrl`, entity id, and
    credential variables; requests and tests reference these variables.
- **Environments & deployment** (production readiness):
  - One `appsettings` per environment — `appsettings.json` (shared),
    `appsettings.Development.json`, `appsettings.Staging.json`,
    `appsettings.Production.json`; Staging/Production secrets are injected via
    environment variables (never committed).
  - CI/CD pipelines: `.github/workflows/ci.yml` (build + test) and
    `.github/workflows/deploy.yml` (parameterized build/push/deploy for
    Development/Staging/Production), plus `deploy/deploy-*.sh` scripts and an
    `azure-pipelines.yml` equivalent.

## 4. Polish & Final Acceptance

- `dotnet build` clean (0 warnings) and `dotnet test` green on Windows/macOS.
- Confirm platform-agnostic paths (no Windows-specific assumptions).
- Verify every assessment requirement is met:
  1. Webhook + Basic auth + batch ingestion.
  2. Validation/sanitization; hard-delete vs unpublish.
  3. EF Core + relational DB; latest-version tracking.
  4. Read-only API + admin visibility + admin disable.
  5. Sync/async decision documented; read/write context split + optimized reads.
  6. Observability logs.
  7. Event-processing + auth tests.
  8. .NET 9, README, platform-agnostic, GitHub repo.

## 5. Acceptance Criteria

- [ ] Structured event logs (received/processed/rejected/failed).
- [ ] README complete with setup + decisions.
- [ ] Architecture docs match implementation.
- [ ] Full suite green; acceptance criteria verified.

## 6. Deferred production considerations

Not implemented for this assessment, but documented (README "Future production
considerations") as expected production work:

- **Read caching** — a distributed cache (Redis) for the query path, with
  event-driven invalidation, wrapped behind `IContentManagementEntityQueries`.
- **Performance & scalability** — database indexes, Npgsql/connection tuning, query
  optimization (`EXPLAIN ANALYZE`, compiled queries, pagination), EF throughput and
  optimistic-concurrency (`xmin`/rowversion) measurement, and horizontal scaling.
- **Infrastructure** — cloud vs. on-premise topology (CI/CD baseline is present),
  secret management, OpenTelemetry traces/metrics beyond Serilog, and a dedicated
  database-migration job for Staging/Production.
