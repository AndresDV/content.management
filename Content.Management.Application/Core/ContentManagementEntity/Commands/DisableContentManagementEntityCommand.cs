using MediatR;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Locally disables a content management entity (admin override).</summary>
public record DisableContentManagementEntityCommand(
    string Id,
    string DisabledBy
) : IRequest<bool>;
