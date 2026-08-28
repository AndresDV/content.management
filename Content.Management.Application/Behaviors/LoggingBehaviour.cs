using MediatR;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Behaviors;

/// <summary>Pipeline behavior that logs request handling and failures.</summary>
public class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            logger.LogInformation("----- Handling request {RequestName} ({@Request})", requestName, request);
            var response = await next(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("----- Handled request {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "----- Error handling request {RequestName}", requestName);
            throw;
        }
    }
}
