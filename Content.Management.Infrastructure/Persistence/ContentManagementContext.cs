using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using Content.Management.Domain.SeedWork;
using Content.Management.Infrastructure.Persistence.EntityConfigurations;
using Content.Management.Infrastructure.Persistence.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Content.Management.Infrastructure.Persistence;

/// <summary>EF Core context for content management. Serves as the unit of work.</summary>
public class ContentManagementContext(
    DbContextOptions<ContentManagementContext> options,
    IMediator mediator) : DbContext(options), IUnitOfWork
{
    public DbSet<ContentManagementEntity> ContentManagementEntities { get; set; } = null!;

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await mediator.DispatchDomainEventsAsync(this).ConfigureAwait(false);

        _ = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ChangeTracker.Clear();

        return true;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContentManagementEntityTypeConfiguration());
    }
}
