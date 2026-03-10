namespace CurrencyConverter.Domain.Events;

/// <summary>
/// Marker interface for all domain events.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
