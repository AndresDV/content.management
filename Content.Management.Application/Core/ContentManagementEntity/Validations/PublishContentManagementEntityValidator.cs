using Content.Management.Application.Core.ContentManagementEntity.Commands;
using FluentValidation;

namespace Content.Management.Application.Core.ContentManagementEntity.Validations;

/// <summary>Validates <see cref="PublishContentManagementEntityCommand"/>.</summary>
public class PublishContentManagementEntityValidator : AbstractValidator<PublishContentManagementEntityCommand>
{
    public PublishContentManagementEntityValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Payload).NotEmpty();
        RuleFor(x => x.Version).GreaterThan(0);
    }
}
