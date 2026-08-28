using Content.Management.Domain.SeedWork;

namespace Content.Management.Api.Authentication;

/// <summary>Authentication scheme names.</summary>
public sealed class AuthenticationSchemes(string key, string name) : Enumeration(key, name)
{
    public static readonly AuthenticationSchemes Organization = new(nameof(Organization), nameof(Organization));

    public static readonly AuthenticationSchemes Users = new(nameof(Users), nameof(Users));
}
