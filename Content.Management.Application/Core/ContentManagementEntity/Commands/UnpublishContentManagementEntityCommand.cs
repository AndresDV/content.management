using MediatR;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Unpublishes a content management entity (retains latest version).</summary>
public record UnpublishContentManagementEntityCommand(string Id, string Payload, int Version) : IRequest<bool>;
