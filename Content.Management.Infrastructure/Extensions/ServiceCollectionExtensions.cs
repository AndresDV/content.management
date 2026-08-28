using Content.Management.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Content.Management.Infrastructure.Extensions;

/// <summary>Registers infrastructure services (DbContexts).</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConfigurationKeys.ContentManagementConnectionString.Key)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConfigurationKeys.ContentManagementConnectionString.Key}' is not configured.");

        services.AddDbContext<ContentManagementContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContext<ContentManagementReadContext>(options =>
            options.UseNpgsql(connectionString).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        return services;
    }
}
