namespace Content.Management.Domain.SeedWork;

/// <summary>Base contract for aggregate repositories.</summary>
/// <typeparam name="T">The aggregate root type.</typeparam>
public interface IRepository<T> where T : IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}
