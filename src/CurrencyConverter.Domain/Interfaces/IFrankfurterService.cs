using CurrencyConverter.Domain.ValueObjects;

namespace CurrencyConverter.Domain.Interfaces;

/// <summary>
/// Abstraction for fetching exchange rate data from an external source (e.g. Frankfurter API).
/// Lives in the Domain layer so the Application layer can depend on it without coupling to
/// infrastructure concerns.
/// </summary>
public interface IFrankfurterService
{
    /// <summary>
    /// Gets the latest exchange rates, optionally specifying a base currency and target currencies.
    /// </summary>
    /// <param name="baseCurrency">The base currency code (default: EUR).</param>
    /// <param name="targetCurrencies">Optional list of target currency codes to filter results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ExchangeRateData> GetLatestRatesAsync(
        string baseCurrency = "EUR",
        IEnumerable<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical exchange rates for the specified date.
    /// </summary>
    /// <param name="date">The historical date.</param>
    /// <param name="baseCurrency">The base currency code (default: EUR).</param>
    /// <param name="targetCurrencies">Optional list of target currency codes to filter results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ExchangeRateData> GetHistoricalRatesAsync(
        DateOnly date,
        string baseCurrency = "EUR",
        IEnumerable<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="fromCurrency">The source currency code.</param>
    /// <param name="toCurrencies">The target currency codes to convert to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ExchangeRateData> ConvertAsync(
        decimal amount,
        string fromCurrency,
        IEnumerable<string> toCurrencies,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available currencies supported by Frankfurter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyDictionary<string, string>> GetAvailableCurrenciesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a snapshot of exchange rate data returned from the Frankfurter API.
/// </summary>
public record ExchangeRateData(
    decimal Amount,
    string Base,
    DateOnly Date,
    IReadOnlyDictionary<string, decimal> Rates);
