using Content.Management.Domain.SeedWork;

namespace Content.Management.Infrastructure.Extensions;

/// <summary>Application configuration keys (connection strings, sections).</summary>
public sealed class ConfigurationKeys(string key, string name) : Enumeration(key, name)
{
    public static readonly ConfigurationKeys ContentManagementConnectionString = new("ContentManagement", nameof(ContentManagementConnectionString));

    public static readonly ConfigurationKeys AuthenticationOrganization = new("Authentication:Organization", nameof(AuthenticationOrganization));

    public static readonly ConfigurationKeys AuthenticationUsers = new("Authentication:Users", nameof(AuthenticationUsers));
}
