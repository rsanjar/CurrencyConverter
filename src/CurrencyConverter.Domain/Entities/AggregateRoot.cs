using CurrencyConverter.Domain.Common;
using CurrencyConverter.Domain.Events;

namespace CurrencyConverter.Domain.Entities;

/// <summary>
/// Base class for all aggregate roots in the domain.
/// Provides domain event handling capabilities.
/// </summary>
public abstract class AggregateRoot : BaseEntity, IAggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
