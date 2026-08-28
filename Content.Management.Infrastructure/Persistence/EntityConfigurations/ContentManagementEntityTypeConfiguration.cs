using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Content.Management.Infrastructure.Persistence.EntityConfigurations;

/// <summary>EF configuration for <see cref="ContentManagementEntity"/>.</summary>
public class ContentManagementEntityTypeConfiguration : IEntityTypeConfiguration<ContentManagementEntity>
{
    public void Configure(EntityTypeBuilder<ContentManagementEntity> entityConfiguration)
    {
        entityConfiguration.ToTable("content_management_entities");

        entityConfiguration.Ignore(e => e.DomainEvents);

        entityConfiguration.HasKey(e => e.Id);
        entityConfiguration.Property(e => e.Id).HasMaxLength(128);

        entityConfiguration.Property(e => e.Payload).IsRequired();
        entityConfiguration.Property(e => e.Version).IsRequired();
        entityConfiguration.Property(e => e.IsPublished).IsRequired();
        entityConfiguration.Property(e => e.IsDisabled).IsRequired();
        entityConfiguration.Property(e => e.DisabledBy).HasMaxLength(128);
        entityConfiguration.Property(e => e.DisabledAt);
    }
}
