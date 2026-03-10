namespace CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

public record HistoricalRatesPageResponse(
    string Base,
    DateOnly StartDate,
    DateOnly EndDate,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<HistoricalRateItemResponse> Items) : IApiResponse;
