using CurrencyConverter.Domain.Common;

namespace CurrencyConverter.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount paired with a currency code.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }

    private Money(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a new <see cref="Money"/> instance.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the amount is negative.</exception>
    public static Money Create(decimal amount, string currencyCode)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        var currency = CurrencyCode.Create(currencyCode);
        return new Money(amount, currency);
    }

    /// <summary>
    /// Returns a new <see cref="Money"/> instance with the converted amount and target currency.
    /// </summary>
    public Money Convert(decimal rate, string targetCurrencyCode)
    {
        if (rate <= 0)
            throw new ArgumentOutOfRangeException(nameof(rate), "Conversion rate must be positive.");

        return Create(Amount * rate, targetCurrencyCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
