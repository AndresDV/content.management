using Content.Management.Application.Extensions;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Handles <see cref="DisableContentManagementEntityCommand"/>.</summary>
public class DisableContentManagementEntityCommandHandler(
    ILogger<DisableContentManagementEntityCommandHandler> logger,
    IContentManagementEntityRepository repository,
    IValidator<DisableContentManagementEntityCommand> validator)
    : IRequestHandler<DisableContentManagementEntityCommand, bool>
{
    public async Task<bool> Handle(DisableContentManagementEntityCommand command, CancellationToken cancellationToken)
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
            logger.LogWarning("A content management entity with id '{Id}' doesn't exist", command.Id);
            return false;
        }

        entity.Disable(command.DisabledBy);

        repository.Update(entity);

        var saved = await repository.UnitOfWork.SaveEntitiesAsync(cancellationToken).ConfigureAwait(false);
        if (!saved)
        {
            logger.LogWarning("Failed to disable the content management entity with id '{Id}'", command.Id);
            return false;
        }

        logger.LogInformation("The content management entity with id '{Id}' was disabled by '{DisabledBy}'", command.Id, command.DisabledBy);

        return true;
    }
}
