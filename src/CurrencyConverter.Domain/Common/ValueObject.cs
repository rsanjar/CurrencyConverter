namespace CurrencyConverter.Domain.Common;

/// <summary>
/// Base class for all value objects in the domain.
/// </summary>
/// <remarks>
/// Value objects are defined by their data, not their identity.
/// Two value objects are equal when all of their equality components are equal.
///
/// Rules for implementing value objects:
/// <list type="bullet">
///   <item>Immutable — every property must be private set or init only.</item>
///   <item>Self-validating — throw from the factory method; never allow an invalid instance to be constructed.</item>
///   <item>No identity — never add an Id property or persist them as independent rows.</item>
///   <item>Side-effect free — value objects must not raise domain events or depend on external services.</item>
/// </list>
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns all components that contribute to equality.
    /// Yield every property that makes two instances of this value object equal.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(0, HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
