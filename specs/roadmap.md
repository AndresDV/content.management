# Content Management — Implementation Roadmap

- **Date:** 2026-08-27
- **Status:** Approved

## Overview

Content Management is a .NET 9 service that ingests CMS lifecycle events via a
webhook, maintains versioned entities with explicit publication state, and exposes
them through a read-only REST API with a local admin disable override.

The heart of the system is distinguishing:

1. the **latest CMS version** (tracked),
2. the **published state** (what normal users see),
3. the **local admin override** (`IsDisabled`).

> The database represents the latest valid CMS state to expose, while preserving
> enough information to distinguish the latest CMS version from the latest
> published version and any local administrative override.

## Global Decisions

| Decision | Value |
|---|---|
| Event vocabulary | `publish` / `unpublish` / `delete` (`publish` = upsert, covers add + update) |
| Webhook | `POST /api/content-management/events` (batch, Basic auth — org creds) |
| Consumer API | `GET /api/content-management/entities`, `GET /api/content-management/entities/{id}`, `POST /api/content-management/entities/{id}/disable` |
| Database | PostgreSQL via Docker |
| Admin visibility | complete (sees everything) |
| Disable model | `IsDisabled` + `DisabledBy` + `DisabledAt` (explicit admin attribution, extensible) |
| Processing | synchronous (async/await for I/O, no queue) — rationale documented in README |
| MediatR | 12.5.0 (license-free) |
| Endpoint style | Minimal API (no controllers) |
| Naming | `Content.Management.*` namespaces, `ContentManagement` type prefix, no "Cms" in any file/class |

## Phases

| Phase | Title | Spec |
|---|---|---|
| 1 | Core event processing | [phase1-event-processing.md](phase1-event-processing.md) |
| 2 | Authentication, authorization, admin disable, read optimization | [phase2-authentication-authorization.md](phase2-authentication-authorization.md) |
| 3 | Observability, documentation, polish | [phase3-observability-documentation.md](phase3-observability-documentation.md) |

## Architecture

```
              ┌─────────────────────────────┐
              │   Content.Management.Api    │  Minimal API endpoints, auth, Serilog, Swagger
              └──────────────┬──────────────┘
                             │ IMediator, queries, commands
              ┌──────────────▼──────────────┐
              │ Content.Management.App...   │  event commands/handlers, queries, validators, behaviors
              └──────┬──────────────┬───────┘
                     │              │ references Domain + Infrastructure
        ┌────────────▼───┐  ┌───────▼──────────────┐
        │ Domain         │  │ Infrastructure        │
        │ entities, VO,  │  │ DbContexts, configs,  │
        │ repo interfaces│  │ repositories, auth    │
        └────────────────┘  └───────┬──────────────┘
                                    │ Npgsql
                             ┌──────▼──────┐
                             │ PostgreSQL  │
                             └─────────────┘
```

**Dependency graph:** Domain → (none); Infrastructure → Domain; Application → Domain,
Infrastructure; Api → Application, Infrastructure, Domain; UnitTests → Application,
Domain, Infrastructure; IntegrationTests → Api, Application, Infrastructure.

## Migration from the CRUD implementation

The initial implementation was structured around generic CRUD. It is replaced as
follows (structure, MediatR, EF/Npgsql, Minimal API, and test infrastructure are
reused):

| Concern | CRUD (removed) | Event (new) |
|---|---|---|
| Domain fields | `Payload`, `CurrentVersion`, `History`, `CreatedBy/UpdatedBy` | `Version`, `Payload`, `IsPublished`, `IsDisabled`, `DisabledBy`, `DisabledAt` |
| Version model | `CurrentVersion++` + capped `History` | `Version` from event + version guard (no history) |
| Commands | `Create`/`Update`/`Delete` | `Publish`/`Unpublish`/`Delete`/`Disable` |
| Endpoints | CRUD `POST/PUT/DELETE` | webhook `POST .../events` + read-only `GET` + admin `disable` |
| Entity config | `OwnsMany(History)` owned table | single table (no history table) |

## Spec File Index

- [roadmap.md](roadmap.md) — this document
- [phase1-event-processing.md](phase1-event-processing.md)
- [phase2-authentication-authorization.md](phase2-authentication-authorization.md)
- [phase3-observability-documentation.md](phase3-observability-documentation.md)

## Deferred production considerations

Deliberately not implemented for this assessment, but expected for a production
release — documented in the README ("Future production considerations"):

- **Auth upgrade** — JWT / OAuth2 / OpenID Connect with a managed provider (e.g.,
  Microsoft Entra ID), replacing the basic two-pool Basic-auth layer.
- **Read caching** — a distributed cache (Redis) for the read path, with
  event-driven invalidation.
- **Performance & scale** — DB indexes, Npgsql/connection tuning, query
  optimization, EF throughput/optimistic-concurrency measurement, and horizontal
  scaling.
- **Infrastructure** — containerization + CI/CD baseline is present (cloud vs.
  on-premise left open); production adds a secret store, OpenTelemetry
  traces/metrics, and a dedicated migration job for higher environments.
