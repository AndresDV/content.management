using Content.Management.Api.Exceptions;
using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Events;
using Content.Management.Application.Core.ContentManagementEntity.Queries;
using Content.Management.Application.Core.ContentManagementEntity.Validations;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using Content.Management.Infrastructure.Extensions;
using Content.Management.Infrastructure.Persistence.Repositories;
using FluentValidation;
using Microsoft.OpenApi.Models;

namespace Content.Management.Api.Extensions;

/// <summary>Registers application services.</summary>
public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                Description = "Basic authentication. Use organization credentials for the " +
                              "events webhook, or user/admin credentials for the entities endpoints."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Basic"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddInfrastructure(configuration);

        services.AddSecurity(configuration);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(PublishContentManagementEntityCommand).Assembly));

        services.AddQueries();
        services.AddValidators();
        services.AddAggregateRepositories();

        return services;
    }

    private static IServiceCollection AddAggregateRepositories(this IServiceCollection services)
    {
        services.AddScoped<IContentManagementEntityRepository, ContentManagementEntityRepository>();

        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddTransient<IContentManagementEntityQueries, ContentManagementEntityQueries>();

        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<PublishContentManagementEntityCommand>, PublishContentManagementEntityValidator>();
        services.AddSingleton<IValidator<UnpublishContentManagementEntityCommand>, UnpublishContentManagementEntityValidator>();
        services.AddSingleton<IValidator<DeleteContentManagementEntityCommand>, DeleteContentManagementEntityValidator>();
        services.AddSingleton<IValidator<DisableContentManagementEntityCommand>, DisableContentManagementEntityValidator>();
        services.AddSingleton<IValidator<ContentEventRequest>, ContentEventRequestValidator>();

        return services;
    }
}
