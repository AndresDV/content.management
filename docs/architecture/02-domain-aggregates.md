# Domain Aggregates

```mermaid
classDiagram
  class Entity {
    +string Id
    +AddDomainEvent(INotification)
    +ClearDomainEvents()
  }
  class Enumeration {
    <<abstract>>
    +string Key
    +string Name
    +GetAll~T~() IEnumerable~T~
    +FromKey~T~(key) T
    +FromName~T~(name) T
  }
  class IAggregateRoot {
    <<interface>>
  }
  class ContentManagementEntity {
    +int Version
    +string Payload
    +bool IsPublished
    +bool IsDisabled
    +string DisabledBy
    +DateTime DisabledAt
    +DateTime CreatedAt
    +DateTime UpdatedAt
    +Publish(version, payload) bool
    +Unpublish(version, payload) bool
    +Delete()
    +Disable(disabledBy)
    +Enable()
    +IsVisibleTo(UserRole) bool
  }
  class UserRole {
    +UserRole$ User
    +UserRole$ Admin
    +FromName(name) UserRole
  }
  class ContentEventType {
    +ContentEventType$ Publish
    +ContentEventType$ Unpublish
    +ContentEventType$ Delete
    +FromKey(key) ContentEventType
  }

  Entity <|-- ContentManagementEntity
  IAggregateRoot <|.. ContentManagementEntity
  Enumeration <|-- UserRole
  Enumeration <|-- ContentEventType
  ContentManagementEntity --> UserRole : visibility
```

- `ContentManagementEntity` is the single aggregate root.
- `Version` tracks the **latest data version**; `IsPublished` tracks the
  **publication state** (they are distinct — see the unpublish corner case).
- `IsDisabled`/`DisabledBy`/`DisabledAt` are the **local admin override** (never
  affects CMS state) and carry the acting admin for extensibility.
- `IsVisibleTo(UserRole)`: `User` → `IsPublished && !IsDisabled`; `Admin` → always.
- No version history is stored — only the latest version is tracked.
- `UserRole` and `ContentEventType` are strongly-typed `Enumeration` subclasses
  (a string `Key` + `Name`), not native enums or raw strings.
