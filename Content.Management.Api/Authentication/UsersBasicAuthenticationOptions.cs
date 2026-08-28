using Content.Management.Domain;
using Microsoft.AspNetCore.Authentication;

namespace Content.Management.Api.Authentication;

/// <summary>Options for the users (consumers) Basic authentication scheme.</summary>
public sealed class UsersBasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public List<ApiUser> ApiUsers { get; set; } = [];
}

/// <summary>A configured API consumer credential with a role.</summary>
public sealed class ApiUser
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = UserRole.User.Name;
}
