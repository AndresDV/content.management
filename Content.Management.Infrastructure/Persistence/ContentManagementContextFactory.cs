using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Content.Management.Infrastructure.Persistence;

/// <summary>Design-time factory used by `dotnet ef` migrations.</summary>
public class ContentManagementContextFactory : IDesignTimeDbContextFactory<ContentManagementContext>
{
    public ContentManagementContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContentManagementContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=contentmanagement;Username=content;Password=content");

        return new ContentManagementContext(optionsBuilder.Options, null!);
    }
}
