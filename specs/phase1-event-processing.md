# Phase 1 — Core Event Processing

- **Date:** 2026-08-27
- **Status:** Approved
- **Scope:** domain model, webhook ingestion, persistence, read-only API

## 1. Goal

Ingest `publish` / `unpublish` / `delete` events from the CMS and expose the
resulting published entities through a read-only REST API — with correct
version/order handling and the unpublish corner case.

## 2. Event Model

The webhook receives a **batch** of events:

```json
[
  { "type": "publish",   "id": "X", "payload": { "name": "My Event" }, "version": 2, "timestamp": "2024-01-01T00:00:00Z" },
  { "type": "delete",    "id": "Y", "timestamp": "2024-01-01T00:00:00Z" },
  { "type": "unpublish", "id": "Z", "payload": { "name": "Other" },     "version": 4, "timestamp": "2024-01-01T00:00:00Z" }
]
```

| type | payload | version | effect |
|---|---|---|---|
| `publish` | required | required | upsert + mark published (covers add *and* update) |
| `unpublish` | required | required | mark unpublished, retain latest version |
| `delete` | — | — | hard delete |

### Version guard

An event with `version < currentVersion` is **ignored** (stale/out-of-order) — the
service never regresses to an older version. Re-delivery of the same version is
idempotent.

### Corner case

An `unpublish` may reference a version that was **never published**. The entity is
created/updated as `IsPublished = false` and persisted — normal users never see it,
no published payload is exposed, and no state is lost.

## 3. Domain

```
ContentManagementEntity : Entity, IAggregateRoot
  Id             string        (CMS entity id)
  Version        int           (latest data version)
  Payload        string        (JSON payload of the latest version)
  IsPublished    bool          (publication state)
  IsDisabled     bool          (local admin override)
  DisabledBy     string?       (admin who disabled; null when not disabled)
  DisabledAt     DateTime?     (when disabled; null when not disabled)
  CreatedAt / UpdatedAt
```

- `Publish(version, payload)` → guard `version < Version`; else set Version/Payload, `IsPublished = true`.
- `Unpublish(version, payload)` → guard; else set Version/Payload, `IsPublished = false`.
- `Delete()` → no-op (hard delete via repository).
- `Disable(disabledBy)` / `Enable()` → local override (fully used in Phase 2).
- `IsVisibleTo(UserRole)` → `User`: `IsPublished && !IsDisabled`; `Admin`: always true.

Strongly-typed enumerations (extending `SeedWork.Enumeration`, which wraps a string
`Key` + `Name`):
- `UserRole`: `User`, `Admin`.
- `ContentEventType`: `publish`, `unpublish`, `delete`.

**No version history is stored** — only the latest version is tracked, per the
assessment's "track the latest data version" requirement.

## 4. Application

Commands (`: IRequest<bool>`), each with a handler (logger + repository + validator):

- `PublishContentManagementEntityCommand(string Id, string Payload, int Version)`
- `UnpublishContentManagementEntityCommand(string Id, string Payload, int Version)`
- `DeleteContentManagementEntityCommand(string Id)`

Queries (read side, plain query class):

- `IContentManagementEntityQueries` → `GetByIdAsync`, `GetAllAsync` (filter `IsPublished` in this phase; role-awareness lands in Phase 2).
- `ContentManagementEntityDto(Id, Payload, Version, IsPublished, CreatedAt, UpdatedAt)`.

Validation (FluentValidation) per command: `Id` required; `Payload` required for
publish/unpublish; `Version` > 0 for publish/unpublish.

Webhook contract:

- `ContentEventRequest(string Type, string Id, JsonElement? Payload, int? Version, DateTimeOffset? Timestamp)`.
- The `Type` string maps to `ContentEventType` (validated via `ContentEventType.IsDefined`,
  resolved via `ContentEventType.FromKey`).
- Validator enforces event-type/version/payload rules; payload is normalized to a
  compact (whitespace-free) JSON string via `JsonSerializer.Serialize` before being
  stored (so pretty-printed inputs are trimmed to a single canonical line).

## 5. Infrastructure

- `ContentManagementContext` (write, tracking) + `IContentManagementEntityRepository`.
- Entity configuration: `Id` string key, `Payload` text, `IsPublished`/`IsDisabled` bool, `DisabledBy`/`DisabledAt` nullable, `CreatedAt`/`UpdatedAt`.
- EF migration (replaces the CRUD `InitialCreate`, drops the history owned table).

## 6. API

Minimal API endpoint mapping (no controllers):

| Operation | Endpoint | Delegates to |
|---|---|---|
| Ingest batch | `POST /api/content-management/events` | validate → dispatch per-event command |
| Get one | `GET /api/content-management/entities/{id}` | `IContentManagementEntityQueries.GetByIdAsync` |
| Get all | `GET /api/content-management/entities` | `IContentManagementEntityQueries.GetAllAsync` |

Ingestion flow: deserialize batch → validate each event → dispatch the matching
command (`publish` → Publish, `unpublish` → Unpublish, `delete` → Delete) → return
`200 OK` (synchronous processing) with `{ "processed": N }`. Invalid batch → `400`
with details.

Status codes: webhook `200` / `400`; read endpoints `200` / `404` (not found or not
visible). `202` is intentionally not used — events are processed synchronously.

### Dependency injection

`ApplicationExtensions.AddApplicationServices` registers services via named `Add*`
methods, following the reference lifecycle conventions:

- `AddInfrastructure` — `ContentManagementContext` (Scoped).
- `AddMediatR` — command handlers + pipeline behaviors (Transient).
- `AddAggregateRepositories` — `IContentManagementEntityRepository` (Scoped).
- `AddQueries` — `IContentManagementEntityQueries` (Transient).
- `AddValidators` — FluentValidation validators (Singleton).

## 7. Testing

**Unit (business rules):**
- publish v1 → entity exists, visible.
- publish v1 → publish v2 → v2 visible.
- unpublish → entity retained, hidden from normal view.
- **corner case:** unpublish of never-published version → persisted, unpublished.
- delete → entity removed.
- **out-of-order:** publish v3 then publish v2 → v3 remains authoritative.
- validators.

**Integration (Testcontainers Postgres):** webhook → DB → query round-trip.

## 8. Acceptance Criteria

- [ ] Batch webhook processes `publish`/`unpublish`/`delete` correctly.
- [ ] Version guard prevents regression; corner case handled.
- [ ] Read API returns only published entities.
- [ ] No version-history persistence.
- [ ] Unit + integration tests green.

## 9. Deferred

- Authentication / authorization (Phase 2).
- Admin disable endpoint (Phase 2).
- Read/write context split (Phase 2).
- Observability logging (Phase 3).
