using ContentEntity = Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate.ContentManagementEntity;
using Content.Management.Application.Core.ContentManagementEntity.Queries.DTOs;
using Content.Management.Domain;
using Content.Management.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Content.Management.Application.Core.ContentManagementEntity.Queries;

/// <summary>Implementation of <see cref="IContentManagementEntityQueries"/> over the read-only context.</summary>
public class ContentManagementEntityQueries(ContentManagementReadContext context) : IContentManagementEntityQueries
{
    public async Task<ContentManagementEntityDto?> GetByIdAsync(string id, UserRole role, CancellationToken cancellationToken)
    {
        var entity = await context.ContentManagementEntities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        return entity.IsVisibleTo(role) ? Map(entity) : null;
    }

    public async Task<IEnumerable<ContentManagementEntityDto>> GetAllAsync(UserRole role, CancellationToken cancellationToken)
    {
        var isAdmin = role.Equals(UserRole.Admin);

        var entities = await context.ContentManagementEntities
            .Where(x => isAdmin || (x.IsPublished && !x.IsDisabled))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(Map).ToList();
    }

    private static ContentManagementEntityDto Map(ContentEntity entity) =>
        new(entity.Id, entity.Payload, entity.Version, entity.IsPublished, entity.CreatedAt, entity.UpdatedAt);
}
