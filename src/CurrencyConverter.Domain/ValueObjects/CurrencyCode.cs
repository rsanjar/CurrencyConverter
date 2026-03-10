using CurrencyConverter.Domain.Common;

namespace CurrencyConverter.Domain.ValueObjects;

/// <summary>
/// Represents a 3-letter ISO 4217 currency code (e.g. USD, EUR, GBP).
/// </summary>
public sealed class CurrencyCode : ValueObject
{
    public string Value { get; }

    private CurrencyCode(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="CurrencyCode"/> from the given string.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid 3-letter currency code.</exception>
    public static CurrencyCode Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != 3 || !normalized.All(char.IsLetter))
            throw new ArgumentException($"'{value}' is not a valid ISO 4217 currency code. Expected a 3-letter code.", nameof(value));

        return new CurrencyCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(CurrencyCode code) => code.Value;
}
