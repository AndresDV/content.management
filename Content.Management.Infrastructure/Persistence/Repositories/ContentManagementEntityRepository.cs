using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;

namespace Content.Management.Infrastructure.Persistence.Repositories;

/// <summary>Repository for <see cref="ContentManagementEntity"/> aggregates.</summary>
public class ContentManagementEntityRepository(ContentManagementContext context)
    : BaseRepository(context), IContentManagementEntityRepository
{
    public async Task<ContentManagementEntity> AddAsync(ContentManagementEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await Context.ContentManagementEntities.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return entry.Entity;
    }

    public ContentManagementEntity Update(ContentManagementEntity entity)
    {
        return Context.ContentManagementEntities.Update(entity).Entity;
    }

    public void Delete(ContentManagementEntity entity)
    {
        Context.Remove(entity);
    }

    public async Task<ContentManagementEntity?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Context.ContentManagementEntities.FindAsync([id], cancellationToken).ConfigureAwait(false);
    }
}
