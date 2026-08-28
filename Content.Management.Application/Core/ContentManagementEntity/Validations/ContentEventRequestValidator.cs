using Content.Management.Application.Core.ContentManagementEntity.Events;
using Content.Management.Domain.AggregatesModel.ContentManagementEntityAggregate;
using FluentValidation;

namespace Content.Management.Application.Core.ContentManagementEntity.Validations;

/// <summary>Validates a raw CMS event received by the webhook.</summary>
public class ContentEventRequestValidator : AbstractValidator<ContentEventRequest>
{
    private const int MaxPayloadLength = 100_000;

    public ContentEventRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(type => ContentEventType.IsDefined(type))
            .WithMessage($"Type must be one of: {string.Join(", ", ContentEventType.GetAll<ContentEventType>().Select(t => t.Key))}.");

        RuleFor(x => x.Id).NotEmpty().MaximumLength(128);

        RuleFor(x => x.Payload)
            .NotNull()
            .When(RequiresPayload)
            .WithMessage("Payload is required for publish and unpublish events.");

        RuleFor(x => x.Payload)
            .Must(payload => payload is null || payload.Value.GetRawText().Length <= MaxPayloadLength)
            .WithMessage($"Payload must not exceed {MaxPayloadLength} characters.");

        RuleFor(x => x.Version)
            .NotNull()
            .GreaterThan(0)
            .When(RequiresVersion)
            .WithMessage("Version must be a positive integer for publish and unpublish events.");
    }

    private static bool RequiresPayload(ContentEventRequest request) => !IsDelete(request.Type);

    private static bool RequiresVersion(ContentEventRequest request) => !IsDelete(request.Type);

    private static bool IsDelete(string type) =>
        string.Equals(type, ContentEventType.Delete.Key, StringComparison.OrdinalIgnoreCase);
}
