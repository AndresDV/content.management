using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Extensions;

/// <summary>Structured logging helpers for command handling and event processing.</summary>
public static class LoggerExtensions
{
    public static void LogCommandHandlingStarted(this ILogger logger, object command)
    {
        logger.LogInformation("----- Handling command {CommandType} ({@Command})", command.GetGenericTypeName(), command);
    }

    public static void LogCommandValidationErrors(this ILogger logger, object command, IEnumerable<ValidationFailure> errors)
    {
        logger.LogWarning("----- Command {CommandType} validation errors: {@Errors}", command.GetGenericTypeName(), errors);
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
