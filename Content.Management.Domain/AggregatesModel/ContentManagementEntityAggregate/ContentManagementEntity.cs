using Content.Management.Domain.SeedWork;

namespace Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;

/// <summary>
/// A versioned content entity with explicit publication state and a local
/// administrative disable override. Tracks the latest data version only (no
/// historical persistence).
/// </summary>
public class ContentManagementEntity : Entity, IAggregateRoot
{
    public int Version { get; private set; }

    public string Payload { get; private set; }

    public bool IsPublished { get; private set; }

    public bool IsDisabled { get; private set; }

    public string? DisabledBy { get; private set; }

    public DateTime? DisabledAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private ContentManagementEntity()
    {
        Payload = string.Empty;
    }

    public ContentManagementEntity(string id, string payload, int version, bool isPublished)
    {
        Id = id;
        Payload = payload;
        Version = version;
        IsPublished = isPublished;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Applies a publish of the given version, unless it is stale.</summary>
    public bool Publish(int version, string payload)
    {
        if (version < Version)
        {
            return false;
        }

        Version = version;
        Payload = payload;
        IsPublished = true;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>Applies an unpublish of the given version, unless it is stale.</summary>
    public bool Unpublish(int version, string payload)
    {
        if (version < Version)
        {
            return false;
        }

        Version = version;
        Payload = payload;
        IsPublished = false;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>Hard delete is performed by the repository; the aggregate exposes a no-op.</summary>
    public void Delete()
    {
    }

    /// <summary>Applies a local administrative disable (does not affect CMS state).</summary>
    public void Disable(string disabledBy)
    {
        IsDisabled = true;
        DisabledBy = disabledBy;
        DisabledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Clears the local administrative disable.</summary>
    public void Enable()
    {
        IsDisabled = false;
        DisabledBy = null;
        DisabledAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsVisibleTo(UserRole role) =>
        role.Equals(UserRole.Admin) || (IsPublished && !IsDisabled);
}
