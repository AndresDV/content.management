using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Extensions;

/// <summary>Structured logging helpers for command handling and event processing.</summary>
public static class LoggerExtensions
{
    public static void LogCommandHandlingStarted(this ILogger logger, IRequest<bool> command)
    {
        logger.LogDebug("Handling command: {CommandName} - {Command}", command.GetType().Name, command);
    }

    public static void LogCommandValidationErrors(this ILogger logger, IRequest<bool> command, IList<ValidationFailure> errors)
    {
        logger.LogError("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", command.GetType().Name, command, errors);
    }

    public static void LogDomainValidationErrors(this ILogger logger, INotification command, IList<ValidationFailure> errors)
    {
        logger.LogError("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", command.GetType().Name, command, errors);
    }

    public static void LogCommandSent(this ILogger logger, IRequest<bool> command)
    {
        logger.LogDebug("Sending command: {CommandName} - {Command}", command.GetType().Name, command);
    }

    public static void LogDomainEventHandlingStarted(this ILogger logger, INotification @event)
    {
        logger.LogDebug("Handling domain event: {EventName} - {@Event}", @event.GetType().Name, @event);
    }

    public static void LogEventReceived(this ILogger logger, string eventType, string entityId, int? version)
    {
        logger.LogInformation(
            "CMS event received. Type={EventType}, EntityId={EntityId}, Version={Version}",
            eventType, entityId, version);
    }

    public static void LogEventRejected(this ILogger logger, string eventType, string entityId, int? version, string reason)
    {
        logger.LogWarning(
            "CMS event rejected. Type={EventType}, EntityId={EntityId}, Version={Version}, Reason={Reason}",
            eventType, entityId, version, reason);
    }

    public static void LogEventProcessed(this ILogger logger, string eventType, string entityId, int? version)
    {
        logger.LogInformation(
            "CMS event processed. Type={EventType}, EntityId={EntityId}, Version={Version}",
            eventType, entityId, version);
    }

    public static void LogEventFailed(this ILogger logger, string eventType, string entityId, int? version)
    {
        logger.LogWarning(
            "CMS event failed. Type={EventType}, EntityId={EntityId}, Version={Version}",
            eventType, entityId, version);
    }

    public static void LogEventFailed(this ILogger logger, string eventType, string entityId, int? version, Exception exception)
    {
        logger.LogError(
            exception,
            "CMS event failed. Type={EventType}, EntityId={EntityId}, Version={Version}",
            eventType, entityId, version);
    }
}
