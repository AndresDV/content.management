using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using Content.Management.Infrastructure.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Content.Management.Infrastructure.Persistence;

/// <summary>
/// Read-only context for optimized queries. Registered with
/// <c>QueryTrackingBehavior.NoTracking</c> and used exclusively by the read side.
/// </summary>
public class ContentManagementReadContext(DbContextOptions<ContentManagementReadContext> options)
    : DbContext(options)
{
    public DbSet<ContentManagementEntity> ContentManagementEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContentManagementEntityTypeConfiguration());
    }
}
