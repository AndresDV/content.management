using System.Security.Claims;
using System.Text.Json;
using Content.Management.Api.Authentication;
using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Events;
using Content.Management.Application.Core.ContentManagementEntity.Queries;
using Content.Management.Application.Core.ContentManagementEntity.Queries.DTOs;
using Content.Management.Application.Extensions;
using Content.Management.Domain;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

namespace Content.Management.Api.Endpoints;

/// <summary>Maps the ingestion webhook and read-only entity endpoints (Minimal API).</summary>
public static class ContentManagementEndpoints
{
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
    
    private static async Task<Results<Ok, IResult>> IngestContentEventsAsync(
        IContentManagementEntityQueries queries,
        IReadOnlyCollection<ContentEventRequest> events,
        CancellationToken cancellationToken)
    {
        try
        {
            await queries.IngestContentEventsAsync(events, cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok();
        }
        catch (ValidationException validationException)
        {
            var errors = validationException.Errors.Any()
                ? validationException.Errors.Select(e => e.ErrorMessage)
                : [validationException.Message];

            return TypedResults.BadRequest(new { errors });
        }
        catch (Exception)
        {
            return TypedResults.Problem($"Error while trying to ingest content events.");
        }
    }

    private static async Task<Results<Ok<ContentManagementEntityDto>, NotFound<string>, IResult>> GetContentManagementEntityByIdAsync(
        IContentManagementEntityQueries queries,
        string id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = ResolveRole(user);
            var response = await queries.GetByIdAsync(id, role, cancellationToken);

            if (response is null)
            {
                return TypedResults.NotFound($"Content management entity with id {id} not found.");
            }

            return TypedResults.Ok(response);
        }
        catch (Exception)
        {
            return TypedResults.Problem($"Error while trying to retrieve content management entity with id {id}.");
        }
    }

    private static async Task<Results<Ok<IEnumerable<ContentManagementEntityDto>>, NotFound<string>, IResult>> GetAllContentManagementEntitiesAsync(
        IContentManagementEntityQueries queries,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = ResolveRole(user);

            var response = await queries.GetAllAsync(role, cancellationToken);

            return TypedResults.Ok(response);
        }
        catch (Exception)
        {
            return TypedResults.Problem($"Error while trying to retrieve content management entities.");
        }
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
