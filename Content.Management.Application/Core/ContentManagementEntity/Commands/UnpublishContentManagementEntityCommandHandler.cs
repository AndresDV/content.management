using ContentEntity = Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate.ContentManagementEntity;
using Content.Management.Application.Extensions;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Handles <see cref="UnpublishContentManagementEntityCommand"/>.</summary>
public class UnpublishContentManagementEntityCommandHandler(
    ILogger<UnpublishContentManagementEntityCommandHandler> logger,
    IContentManagementEntityRepository repository,
    IValidator<UnpublishContentManagementEntityCommand> validator)
    : IRequestHandler<UnpublishContentManagementEntityCommand, bool>
{
    public async Task<bool> Handle(UnpublishContentManagementEntityCommand command, CancellationToken cancellationToken)
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
            // Corner case: the entity (or this version) was never published.
            entity = new ContentEntity(command.Id, command.Payload, command.Version, isPublished: false);
            await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        else if (!entity.Unpublish(command.Version, command.Payload))
        {
            logger.LogInformation(
                "Ignored stale unpublish event for entity '{Id}' (version {Version})",
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
            logger.LogWarning("Failed to process unpublish event for entity '{Id}'", command.Id);
            return false;
        }

        logger.LogEventProcessed(ContentEventType.Unpublish.Key, command.Id, command.Version);

        return true;
    }
}
