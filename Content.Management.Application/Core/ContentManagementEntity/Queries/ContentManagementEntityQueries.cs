using System.Text.Json;
using Content.Management.Application.Core.ContentManagementEntity.Commands;
using Content.Management.Application.Core.ContentManagementEntity.Events;
using ContentEntity = Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate.ContentManagementEntity;
using Content.Management.Application.Core.ContentManagementEntity.Queries.DTOs;
using Content.Management.Application.Extensions;
using Content.Management.Domain;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using Content.Management.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Core.ContentManagementEntity.Queries;

/// <summary>Implementation of <see cref="IContentManagementEntityQueries"/> over the read-only context.</summary>
public class ContentManagementEntityQueries(
    ContentManagementReadContext context,
    ILogger<ContentManagementEntityQueries> logger,
    IValidator<ContentEventRequest> validator,
    IMediator mediator) : IContentManagementEntityQueries
{
    private const int MaxBatchSize = 1000;
    
    public async Task<ContentManagementEntityDto?> GetByIdAsync(string id, UserRole role, CancellationToken cancellationToken)
    {
        var entity = await context.ContentManagementEntities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        return entity.IsVisibleTo(role) ? BuildContentManagementEntityDto(entity) : null;
    }

    public async Task<IEnumerable<ContentManagementEntityDto>> GetAllAsync(UserRole role, CancellationToken cancellationToken)
    {
        var isAdmin = role.Equals(UserRole.Admin);

        var entities = await context.ContentManagementEntities
            .Where(x => isAdmin || (x.IsPublished && !x.IsDisabled))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(BuildContentManagementEntityDto);
    }

    public async Task IngestContentEventsAsync(IReadOnlyCollection<ContentEventRequest> events, CancellationToken cancellationToken)
    {
        if (events.Count > MaxBatchSize)
        {
            throw new ValidationException($"The batch size exceeds the maximum limit of {MaxBatchSize}.");
        }

        var validationErrors = await ValidateContentManagementCommands(events, cancellationToken);

        if (validationErrors.Count != 0)
        {
            throw new ValidationException("Invalid events were found in the batch.", validationErrors);
        }

        await SendContentManagementCommands(events, cancellationToken);
    }

    private async Task<List<ValidationFailure>> ValidateContentManagementCommands(
        IReadOnlyCollection<ContentEventRequest> events,
        CancellationToken cancellationToken)
    {
        var validationErrors = new List<ValidationFailure>();

        foreach (var contentEvent in events)
        {
            logger.LogEventReceived(contentEvent.Type, contentEvent.Id, contentEvent.Version);

            var result = await validator.ValidateAsync(contentEvent, cancellationToken);
            if (result.IsValid)
            {
                continue;
            }

            foreach (var error in result.Errors)
            {
                logger.LogEventRejected(contentEvent.Type, contentEvent.Id, contentEvent.Version, error.ErrorMessage);
                validationErrors.Add(error);
            }
        }

        return validationErrors;
    }

    private async Task SendContentManagementCommands(IReadOnlyCollection<ContentEventRequest> events, CancellationToken cancellationToken)
    {
        foreach (var contentEvent in events)
        {
            var command = BuildContentManagementCommand(contentEvent);

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
    }

    private static IRequest<bool> BuildContentManagementCommand(ContentEventRequest contentEvent)
    {
        var eventType = ContentEventType.FromKey(contentEvent.Type);

        return eventType.Name switch
        {
            nameof(ContentEventType.Publish) => new PublishContentManagementEntityCommand(
                contentEvent.Id,
                JsonSerializer.Serialize(contentEvent.Payload!.Value),
                contentEvent.Version!.Value),
            nameof(ContentEventType.Unpublish) => new UnpublishContentManagementEntityCommand(
                contentEvent.Id,
                JsonSerializer.Serialize(contentEvent.Payload!.Value),
                contentEvent.Version!.Value),
            nameof(ContentEventType.Delete) => new DeleteContentManagementEntityCommand(contentEvent.Id),
            _ => throw new ArgumentOutOfRangeException($"Unsupported event type: {contentEvent.Type}")
        };
    }

    private static ContentManagementEntityDto BuildContentManagementEntityDto(ContentEntity entity) => new(
        entity.Id,
        entity.Payload,
        entity.Version,
        entity.IsPublished,
        entity.CreatedAt,
        entity.UpdatedAt
    );
}
