# Phase 2 — Authentication, Authorization, Admin Disable, Read Optimization

- **Date:** 2026-08-27
- **Status:** Approved
- **Scope:** two Basic-auth pools, role-based visibility, admin disable, read/write context split

## 1. Goal

Add two independent Basic-auth boundaries, role-based visibility, the admin disable
override, and a read/write DbContext split with optimized read queries.

## 2. Authentication

Two Basic-auth schemes (sharing a `BasicAuthenticationHandler<TOptions>` base),
each with independent credentials, plus three authorization policies:

| Scheme | Handler | Audience | Credentials |
|---|---|---|---|
| `Organization` | `OrganizationBasicAuthenticationHandler` | CMS → webhook (`POST .../events`) | username 10–20 chars, password = GUID |
| `Users` | `UsersBasicAuthenticationHandler` | API consumers | username/email + password + role |

Policies: `Organization` (webhook), `Users` (read endpoints), `Admin`
(`Users` scheme + `Admin` role).

Scheme names, policy names, and configuration keys are strongly-typed
`Enumeration` subclasses (rather than magic strings):
`AuthenticationSchemes` (`Organization`, `Users`), `AuthorizationPolicies`
(`Organization`, `Users`, `Admin`), and `ConfigurationKeys` (connection-string name
and `Authentication:*` sections).

Configuration (via `AddSecurity`, bound from configuration — `appsettings.Development.json`
for local development, overridable by environment variables):
- `Authentication:Organization` → `Username`, `Password`.
- `Authentication:Users:ApiUsers` → list of `{ Username, Password, Role }`.

Development example credentials (Lateral Group domain): organization
`content-integration` (GUID password); users `content.consumer@lateralgroup.com`
(`User`) and `content.admin@lateralgroup.com` (`Admin`).

An **admin** is any configured user with `Role: "Admin"`. The handlers emit a
`ClaimTypes.Role` claim; the `Admin` policy enforces `RequireRole("Admin")`.
Invalid/missing credentials → `401`; authenticated-but-unauthorized → `403`.

**Deferred (future):** Basic auth is an intentionally minimal layer for this
assessment. A production release would replace/augment it with JWT bearer tokens
and OAuth 2.0 / OpenID Connect via a managed identity provider (e.g., Microsoft
Entra ID), plus scoped RBAC — see the README's "Future production considerations".

## 3. Authorization & Visibility

The read-side query interface becomes role-aware:

- `IContentManagementEntityQueries.GetByIdAsync(string id, UserRole role, ...)`
- `IContentManagementEntityQueries.GetAllAsync(UserRole role, ...)`

Visibility (from `ContentManagementEntity.IsVisibleTo`):

| Role | Sees |
|---|---|
| `User` | `IsPublished && !IsDisabled` |
| `Admin` | everything (complete visibility) |

## 4. Admin Disable

`POST /api/content-management/entities/{id}/disable` (admin only):

- Maps to `DisableContentManagementEntityCommand(string Id, string DisabledBy)`.
- `DisabledBy` = the authenticated admin (from the Basic-auth principal).
- Sets `IsDisabled = true`, records `DisabledBy` and `DisabledAt`.
- **Local override only** — it never affects the CMS (no data is sent back).

Status codes: `204 No Content` on success; `404` when the entity is absent; `403`
when authenticated as a non-admin (and `401` when unauthenticated).

## 5. Read Optimization

- Add `ContentManagementReadContext` with `QueryTrackingBehavior.NoTracking`.
- Queries run against the read context (no-tracking default) with projections to DTOs.

Write path (`ContentManagementContext`) keeps tracking + repository/UoW.

## 6. Testing

**Auth:**
- valid org credentials → `200`; invalid/missing → `401`.
- valid user credentials → `200`; invalid/missing → `401`.

**Authorization / visibility:**
- normal user sees published only; admin sees published + unpublished.
- admin disable hides an entity from normal users; admin still sees it.

**Integration (Testcontainers Postgres):** full webhook → query with auth; disable
round-trip.

## 7. Acceptance Criteria

- [ ] Two independent Basic-auth boundaries.
- [ ] Role-correct visibility for `User` and `Admin`.
- [ ] Admin disable is a local override (CMS data untouched).
- [ ] Read context uses `NoTracking`; read queries optimized.
- [ ] Auth + visibility tests green.

## 8. Deferred

- Observability logging (Phase 3).
- README/documentation (Phase 3).
