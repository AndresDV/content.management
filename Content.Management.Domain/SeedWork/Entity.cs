using MediatR;

namespace Content.Management.Domain.SeedWork;

/// <summary>
/// Base class for all domain entities. Provides a string identifier and a
/// per-entity collection of domain events to be dispatched on persistence.
/// </summary>
public abstract class Entity
{
    public string Id { get; set; } = string.Empty;

    private List<INotification>? _domainEvents;

    public IReadOnlyCollection<INotification>? DomainEvents => _domainEvents?.AsReadOnly();

    protected void AddDomainEvent(INotification eventItem)
    {
        _domainEvents ??= [];
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem) => _domainEvents?.Remove(eventItem);

    public void ClearDomainEvents() => _domainEvents?.Clear();
}
