namespace CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

/// <summary>
/// Response model for a currency conversion result.
/// </summary>
public record ConversionResponse(
    decimal Amount,
    string From,
    DateOnly Date,
    IReadOnlyDictionary<string, decimal> Rates) : IApiResponse;
