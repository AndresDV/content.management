using Content.Management.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Content.Management.Api.Extensions;

/// <summary>Ensures the database schema is up to date in local environments.</summary>
public static class DatabaseExtensions
{
    public static void MigrateDatabase(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContentManagementContext>();
        context.Database.Migrate();
    }
}
