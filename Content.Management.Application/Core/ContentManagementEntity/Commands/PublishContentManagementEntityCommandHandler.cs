using ContentEntity = Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate.ContentManagementEntity;
using Content.Management.Application.Extensions;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Handles <see cref="PublishContentManagementEntityCommand"/>.</summary>
public class PublishContentManagementEntityCommandHandler(
    ILogger<PublishContentManagementEntityCommandHandler> logger,
    IContentManagementEntityRepository repository,
    IValidator<PublishContentManagementEntityCommand> validator)
    : IRequestHandler<PublishContentManagementEntityCommand, bool>
{
    public async Task<bool> Handle(PublishContentManagementEntityCommand command, CancellationToken cancellationToken)
    {
        logger.LogCommandHandlingStarted(command);

        var result = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (result.Errors.Count != 0)
        {
            logger.LogCommandValidationErrors(command, result.Errors);
            throw new ValidationException($"Command Validation Errors for type {command.GetGenericTypeName()}", result.Errors);
        }

        var entity = await repository.FindAsync(command.Id, cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            entity = new ContentEntity(command.Id, command.Payload, command.Version, isPublished: true);
            await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        else if (!entity.Publish(command.Version, command.Payload))
        {
            logger.LogInformation(
                "Ignored stale publish event for entity '{Id}' (version {Version})",
                command.Id,
                command.Version);

            return true;
        }
        else
        {
            repository.Update(entity);
        }

        var saved = await repository.UnitOfWork.SaveEntitiesAsync(cancellationToken).ConfigureAwait(false);
        if (!saved)
        {
            logger.LogWarning("Failed to process publish event for entity '{Id}'", command.Id);
            return false;
        }

        logger.LogEventProcessed(ContentEventType.Publish.Key, command.Id, command.Version);

        return true;
    }
}
