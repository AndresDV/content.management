using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Content.Management.Api.Authentication;

/// <summary>Basic authentication for API consumers (users).</summary>
public sealed class UsersBasicAuthenticationHandler(
    IOptionsMonitor<UsersBasicAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : BasicAuthenticationHandler<UsersBasicAuthenticationOptions>(options, logger, encoder)
{
    protected override ClaimsPrincipal? ValidateCredentials(string username, string password)
    {
        var user = Options.ApiUsers.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.Ordinal) &&
            string.Equals(u.Password, password, StringComparison.Ordinal));

        if (user is null)
        {
            return null;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
    }
}
