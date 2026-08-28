using Content.Management.Domain.SeedWork;

namespace Content.Management.Api.Authentication;

/// <summary>Authorization policy names.</summary>
public sealed class AuthorizationPolicies(string key, string name) : Enumeration(key, name)
{
    public static readonly AuthorizationPolicies Organization = new(nameof(Organization), nameof(Organization));

    public static readonly AuthorizationPolicies Users = new(nameof(Users), nameof(Users));

    public static readonly AuthorizationPolicies Admin = new(nameof(Admin), nameof(Admin));
}
