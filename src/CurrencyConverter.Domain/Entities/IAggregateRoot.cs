using CurrencyConverter.Domain.Common;
using CurrencyConverter.Domain.Events;

namespace CurrencyConverter.Domain.Entities;

/// <summary>
/// Marker interface for aggregate roots in the domain model.
/// Aggregate roots are consistency boundaries that can be directly persisted.
/// Only aggregate roots should be used with write repositories.
/// </summary>
public interface IAggregateRoot : IBaseEntity
{
    /// <summary>
    /// Collection of domain events raised by this aggregate.
    /// </summary>
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from the aggregate.
    /// </summary>
    void ClearDomainEvents();
}
