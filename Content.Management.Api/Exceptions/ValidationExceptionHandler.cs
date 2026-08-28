using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Content.Management.Api.Exceptions;

/// <summary>Maps <see cref="ValidationException"/> to a 400 Bad Request problem response.</summary>
public class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/problem+json";

        var errors = validationException.Errors
            .Select(e => e.ErrorMessage)
            .ToList();

        await httpContext.Response.WriteAsJsonAsync(new { errors }, cancellationToken);

        return true;
    }
}
