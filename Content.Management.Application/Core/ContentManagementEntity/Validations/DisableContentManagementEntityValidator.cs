using Content.Management.Application.Core.ContentManagementEntity.Commands;
using FluentValidation;

namespace Content.Management.Application.Core.ContentManagementEntity.Validations;

/// <summary>Validates <see cref="DisableContentManagementEntityCommand"/>.</summary>
public class DisableContentManagementEntityValidator : AbstractValidator<DisableContentManagementEntityCommand>
{
    public DisableContentManagementEntityValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DisabledBy).NotEmpty().MaximumLength(128);
    }
}
