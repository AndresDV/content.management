using Content.Management.Domain.SeedWork;

namespace Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;

/// <summary>The type of a CMS event received by the ingestion webhook.</summary>
public sealed class ContentEventType(string key, string name) : Enumeration(key, name)
{
    public static readonly ContentEventType Publish = new("publish", nameof(Publish));

    public static readonly ContentEventType Unpublish = new("unpublish", nameof(Unpublish));

    public static readonly ContentEventType Delete = new("delete", nameof(Delete));

    public static bool IsDefined(string key) =>
        GetAll<ContentEventType>().Any(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));

    public static ContentEventType FromKey(string key) =>
        GetAll<ContentEventType>().SingleOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Possible values for {nameof(ContentEventType)}: {string.Join(", ", GetAll<ContentEventType>().Select(t => t.Key))}");
}
