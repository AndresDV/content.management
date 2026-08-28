using Content.Management.Domain.SeedWork;
using Content.Management.Infrastructure.Persistence;

namespace Content.Management.Infrastructure.Persistence.Repositories;

/// <summary>Base class for repositories, exposing the context and unit of work.</summary>
public class BaseRepository(ContentManagementContext context)
{
    protected readonly ContentManagementContext Context = context;

    public IUnitOfWork UnitOfWork => Context;
}
