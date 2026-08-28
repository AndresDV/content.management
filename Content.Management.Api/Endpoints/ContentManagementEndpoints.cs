using System.Security.Claims;
using System.Text.Json;
using Content.Management.Api.Authentication;
using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Events;
using Content.Management.Application.Core.ContentManagementEntity.Queries;
using Content.Management.Application.Extensions;
using Content.Management.Domain;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Content.Management.Api.Endpoints;

/// <summary>Maps the ingestion webhook and read-only entity endpoints (Minimal API).</summary>
public static class ContentManagementEndpoints
{
    private const int MaxBatchSize = 1000;

    public static IEndpointRouteBuilder MapContentManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/content-management");

        group.MapPost("events", IngestContentEventsAsync)
            .RequireAuthorization(AuthorizationPolicies.Organization.Key);

        group.MapGet("entities", GetAllContentManagementEntitiesAsync)
            .RequireAuthorization(AuthorizationPolicies.Users.Key);

        group.MapGet("entities/{id}", GetContentManagementEntityByIdAsync)
            .RequireAuthorization(AuthorizationPolicies.Users.Key);

        group.MapPost("entities/{id}/disable", DisableContentManagementEntityAsync)
            .RequireAuthorization(AuthorizationPolicies.Admin.Key);

        return endpoints;
    }

    private static async Task<IResult> IngestContentEventsAsync(
        IMediator mediator,
        IValidator<ContentEventRequest> validator,
        ILoggerFactory loggerFactory,
        List<ContentEventRequest> events,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("ContentManagementEndpoints");
        var errors = new List<string>();

        if (events.Count > MaxBatchSize)
        {
            return Results.BadRequest(new { error = $"Batch size must not exceed {MaxBatchSize} events." });
        }

        foreach (var contentEvent in events)
        {
            logger.LogEventReceived(contentEvent.Type, contentEvent.Id, contentEvent.Version);

            var result = await validator.ValidateAsync(contentEvent, cancellationToken);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    logger.LogEventRejected(contentEvent.Type, contentEvent.Id, contentEvent.Version, error.ErrorMessage);
                    errors.Add(error.ErrorMessage);
                }
            }
        }

        if (errors.Count != 0)
        {
            return Results.BadRequest(new { errors });
        }

        foreach (var contentEvent in events)
        {
            var command = ToCommand(contentEvent);

            try
            {
                await mediator.Send(command, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogEventFailed(contentEvent.Type, contentEvent.Id, contentEvent.Version, ex);
                throw;
            }
        }

        return Results.Ok(new { processed = events.Count });
    }

    private static IRequest<bool> ToCommand(ContentEventRequest contentEvent)
    {
        var eventType = ContentEventType.FromKey(contentEvent.Type);

        if (eventType.Equals(ContentEventType.Publish))
        {
            return new PublishContentManagementEntityCommand(
                contentEvent.Id,
                JsonSerializer.Serialize(contentEvent.Payload!.Value),
                contentEvent.Version!.Value);
        }

        if (eventType.Equals(ContentEventType.Unpublish))
        {
            return new UnpublishContentManagementEntityCommand(
                contentEvent.Id,
                JsonSerializer.Serialize(contentEvent.Payload!.Value),
                contentEvent.Version!.Value);
        }

        return new DeleteContentManagementEntityCommand(contentEvent.Id);
    }

    private static async Task<IResult> GetContentManagementEntityByIdAsync(
        IContentManagementEntityQueries queries,
        string id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var role = ResolveRole(user);

        var dto = await queries.GetByIdAsync(id, role, cancellationToken);

        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> GetAllContentManagementEntitiesAsync(
        IContentManagementEntityQueries queries,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var role = ResolveRole(user);

        var dtos = await queries.GetAllAsync(role, cancellationToken);

        return Results.Ok(dtos);
    }

    private static async Task<IResult> DisableContentManagementEntityAsync(
        IMediator mediator,
        string id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var disabledBy = user.Identity?.Name ?? "unknown";

        var disabled = await mediator.Send(
            new DisableContentManagementEntityCommand(id, disabledBy),
            cancellationToken);

        return disabled ? Results.NoContent() : Results.NotFound();
    }

    private static UserRole ResolveRole(ClaimsPrincipal user) =>
        user.IsInRole(UserRole.Admin.Name) ? UserRole.Admin : UserRole.User;
}
