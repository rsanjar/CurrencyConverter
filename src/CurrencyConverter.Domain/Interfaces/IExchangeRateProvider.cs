using CurrencyConverter.Domain.ValueObjects;

namespace CurrencyConverter.Domain.Interfaces;

/// <summary>
/// Provider-agnostic abstraction for fetching exchange-rate data.
/// Lives in the Domain layer so Application handlers stay decoupled from
/// any concrete HTTP client or third-party API.
/// <para>
/// Implement this interface once per external data source and register the
/// implementation with <see cref="IExchangeRateProviderFactory"/> to make it
/// available throughout the application.
/// </para>
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>Gets the latest exchange rates for the given base currency.</summary>
    Task<ExchangeRateData> GetLatestRatesAsync(
        string baseCurrency = "EUR",
        IEnumerable<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets historical exchange rates for the specified date.</summary>
    Task<ExchangeRateData> GetHistoricalRatesAsync(
        DateOnly date,
        string baseCurrency = "EUR",
        IEnumerable<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default);

    /// <summary>Converts an amount between currencies.</summary>
    Task<ExchangeRateData> ConvertAsync(
        decimal amount,
        string fromCurrency,
        IEnumerable<string> toCurrencies,
        CancellationToken cancellationToken = default);

    /// <summary>Gets all currencies supported by this provider.</summary>
    Task<IReadOnlyDictionary<string, string>> GetAvailableCurrenciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Gets historical exchange rates for a date range.</summary>
    Task<HistoricalRatesRangeData> GetHistoricalRatesRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        string baseCurrency = "EUR",
        IEnumerable<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a snapshot of exchange rate data returned from a provider.
/// </summary>
public record ExchangeRateData(
    decimal Amount,
    string Base,
    DateOnly Date,
    IReadOnlyDictionary<string, decimal> Rates);

/// <summary>
/// Represents a historical range of exchange rates returned from a provider.
/// </summary>
public record HistoricalRatesRangeData(
    decimal Amount,
    string Base,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyDictionary<DateOnly, IReadOnlyDictionary<string, decimal>> RatesByDate);
