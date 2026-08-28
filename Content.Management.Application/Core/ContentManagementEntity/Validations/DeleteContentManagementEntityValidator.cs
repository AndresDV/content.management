using Content.Management.Application.Core.ContentManagementEntity.Commands;
using FluentValidation;

namespace Content.Management.Application.Core.ContentManagementEntity.Validations;

/// <summary>Validates <see cref="DeleteContentManagementEntityCommand"/>.</summary>
public class DeleteContentManagementEntityValidator : AbstractValidator<DeleteContentManagementEntityCommand>
{
    public DeleteContentManagementEntityValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(128);
    }
}
