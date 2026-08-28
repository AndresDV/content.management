using System.Text.Json;

namespace Content.Management.Application.Core.ContentManagementEntity.Events;

/// <summary>A single CMS event received by the ingestion webhook.</summary>
public sealed record ContentEventRequest(
    string Type,
    string Id,
    JsonElement? Payload,
    int? Version,
    DateTimeOffset? Timestamp
);
