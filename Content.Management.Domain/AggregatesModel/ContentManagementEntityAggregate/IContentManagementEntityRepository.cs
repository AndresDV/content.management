using Content.Management.Domain.SeedWork;

namespace Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;

/// <summary>Repository contract for <see cref="ContentManagementEntity"/> aggregates.</summary>
public interface IContentManagementEntityRepository : IRepository<ContentManagementEntity>
{
    Task<ContentManagementEntity> AddAsync(ContentManagementEntity entity, CancellationToken cancellationToken = default);

    ContentManagementEntity Update(ContentManagementEntity entity);

    void Delete(ContentManagementEntity entity);

    Task<ContentManagementEntity?> FindAsync(string id, CancellationToken cancellationToken = default);
}
