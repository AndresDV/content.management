using MediatR;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Publishes a new version of a content management entity (upsert).</summary>
public record PublishContentManagementEntityCommand(
    string Id,
    string Payload,
    int Version
) : IRequest<bool>;
