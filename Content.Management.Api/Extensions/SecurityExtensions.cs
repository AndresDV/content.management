using Content.Management.Api.Authentication;
using Content.Management.Domain;
using Content.Management.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication;

namespace Content.Management.Api.Extensions;

/// <summary>Registers authentication schemes and authorization policies.</summary>
public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddScheme<OrganizationBasicAuthenticationOptions, OrganizationBasicAuthenticationHandler>(
                AuthenticationSchemes.Organization.Key,
                options => configuration.GetSection(ConfigurationKeys.AuthenticationOrganization.Key).Bind(options))
            .AddScheme<UsersBasicAuthenticationOptions, UsersBasicAuthenticationHandler>(
                AuthenticationSchemes.Users.Key,
                options => configuration.GetSection(ConfigurationKeys.AuthenticationUsers.Key).Bind(options));

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.Organization.Key, policy => policy
                .AddAuthenticationSchemes(AuthenticationSchemes.Organization.Key)
                .RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicies.Users.Key, policy => policy
                .AddAuthenticationSchemes(AuthenticationSchemes.Users.Key)
                .RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicies.Admin.Key, policy => policy
                .AddAuthenticationSchemes(AuthenticationSchemes.Users.Key)
                .RequireRole(UserRole.Admin.Name));
        });

        return services;
    }
}
