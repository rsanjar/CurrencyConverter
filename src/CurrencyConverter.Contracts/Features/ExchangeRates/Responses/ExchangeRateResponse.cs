namespace CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

/// <summary>
/// Response model for exchange rate data.
/// </summary>
public record ExchangeRateResponse(
    decimal Amount,
    string Base,
    DateOnly Date,
    IReadOnlyDictionary<string, decimal> Rates) : IApiResponse;
