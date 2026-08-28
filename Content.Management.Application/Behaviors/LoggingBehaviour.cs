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
        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling the command: {CommandName}", typeof(TRequest).Name);
            throw;
        }
    }
}
