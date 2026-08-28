# System Context

```mermaid
C4Context
  title System Context — Content Management

  System_Ext(cms, "CMS", "Sends content lifecycle events (publish/unpublish/delete)")
  Person(user, "Consumer (User)", "Reads published entities")
  Person(admin, "Consumer (Admin)", "Reads all entities; locally disables entities")

  System(svc, "Content Management API", ".NET 9 service: ingests events, tracks versioned publication state, serves a read-only API")
  SystemDb(db, "PostgreSQL (Docker)", "Stores entities and their publication state")

  Rel(cms, svc, "POST /api/content-management/events", "REST/JSON · Basic auth (org)")
  Rel(user, svc, "GET /api/content-management/entities", "REST/JSON · Basic auth (user)")
  Rel(admin, svc, "GET entities + POST disable", "REST/JSON · Basic auth (admin)")
  Rel(svc, db, "reads/writes", "EF Core (Npgsql)")
```

The service has two independent Basic-auth boundaries: the CMS organization (webhook)
and API consumers (users vs. admin). Consumers are read-only; the CMS is the source
of truth.
