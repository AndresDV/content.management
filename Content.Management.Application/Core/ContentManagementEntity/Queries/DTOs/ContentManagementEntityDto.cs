namespace Content.Management.Application.Core.ContentManagementEntity.Queries.DTOs;

/// <summary>Read model for a content management entity.</summary>
public sealed record ContentManagementEntityDto(
    string Id,
    string Payload,
    int Version,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt);
