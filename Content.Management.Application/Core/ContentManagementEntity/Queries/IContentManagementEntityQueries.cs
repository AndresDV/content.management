using Content.Management.Application.Core.ContentManagementEntity.Events;
using Content.Management.Application.Core.ContentManagementEntity.Queries.DTOs;
using Content.Management.Domain;

namespace Content.Management.Application.Core.ContentManagementEntity.Queries;

/// <summary>Read-side queries for content management entities.</summary>
public interface IContentManagementEntityQueries
{
    Task<ContentManagementEntityDto?> GetByIdAsync(string id, UserRole role, CancellationToken cancellationToken);

    Task<IEnumerable<ContentManagementEntityDto>> GetAllAsync(UserRole role, CancellationToken cancellationToken);
    
    Task IngestContentEventsAsync(IReadOnlyCollection<ContentEventRequest> events, CancellationToken cancellationToken);
}
