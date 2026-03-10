namespace CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

public record HistoricalRateItemResponse(
    DateOnly Date,
    IReadOnlyDictionary<string, decimal> Rates);
