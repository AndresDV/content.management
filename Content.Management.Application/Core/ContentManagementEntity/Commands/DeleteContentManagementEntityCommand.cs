using MediatR;

namespace Content.Management.Application.Core.ContentManagementEntity.Commands;

/// <summary>Deletes a content management entity.</summary>
public record DeleteContentManagementEntityCommand(string Id) : IRequest<bool>;
