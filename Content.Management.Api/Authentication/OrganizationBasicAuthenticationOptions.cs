using Microsoft.AspNetCore.Authentication;

namespace Content.Management.Api.Authentication;

/// <summary>Options for the organization (CMS) Basic authentication scheme.</summary>
public sealed class OrganizationBasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
