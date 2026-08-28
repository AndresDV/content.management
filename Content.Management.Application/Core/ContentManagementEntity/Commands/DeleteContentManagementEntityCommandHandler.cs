using Content.Management.Application.Extensions;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Handles <see cref="DeleteContentManagementEntityCommand"/>.</summary>
public class DeleteContentManagementEntityCommandHandler(
    ILogger<DeleteContentManagementEntityCommandHandler> logger,
    IContentManagementEntityRepository repository,
    IValidator<DeleteContentManagementEntityCommand> validator)
    : IRequestHandler<DeleteContentManagementEntityCommand, bool>
{
    public async Task<bool> Handle(DeleteContentManagementEntityCommand command, CancellationToken cancellationToken)
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
            logger.LogInformation("Delete event for non-existent entity '{Id}' — treated as a no-op", command.Id);
            return true;
        }

        entity.Delete();

        repository.Delete(entity);

        var saved = await repository.UnitOfWork.SaveEntitiesAsync(cancellationToken).ConfigureAwait(false);
        if (!saved)
        {
            logger.LogWarning("Failed to process delete event for entity '{Id}'", command.Id);
            return false;
        }

        logger.LogEventProcessed(ContentEventType.Delete.Key, command.Id, null);

        return true;
    }
}
