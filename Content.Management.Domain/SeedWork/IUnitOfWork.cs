namespace Content.Management.Domain.SeedWork;

/// <summary>Unit of work contract wrapping the persistence transaction boundary.</summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
