using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Content.Management.Api.Authentication;

/// <summary>
/// Base Basic authentication handler. Decodes the <c>Authorization: Basic</c> header
/// and delegates credential validation to the derived handler.
/// </summary>
public abstract class BasicAuthenticationHandler<TOptions>(
    IOptionsMonitor<TOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TOptions>(options, logger, encoder)
    where TOptions : AuthenticationSchemeOptions, new()
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var headerValue = header.ToString();
        if (!headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var principal = DecodeAndValidate(headerValue["Basic ".Length..].Trim());

        if (principal is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));
        }

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected abstract ClaimsPrincipal? ValidateCredentials(string username, string password);

    private ClaimsPrincipal? DecodeAndValidate(string token)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var separator = decoded.IndexOf(':');
            if (separator < 0)
            {
                return null;
            }

            var username = decoded[..separator];
            var password = decoded[(separator + 1)..];

            return ValidateCredentials(username, password);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
