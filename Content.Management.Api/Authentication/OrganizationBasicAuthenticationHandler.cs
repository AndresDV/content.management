using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Content.Management.Api.Authentication;

/// <summary>Basic authentication for the CMS organization (webhook).</summary>
public sealed class OrganizationBasicAuthenticationHandler(
    IOptionsMonitor<OrganizationBasicAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : BasicAuthenticationHandler<OrganizationBasicAuthenticationOptions>(options, logger, encoder)
{
    protected override ClaimsPrincipal? ValidateCredentials(string username, string password)
    {
        if (username != Options.Username || password != Options.Password)
        {
            return null;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, AuthenticationSchemes.Organization.Name)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
    }
}
