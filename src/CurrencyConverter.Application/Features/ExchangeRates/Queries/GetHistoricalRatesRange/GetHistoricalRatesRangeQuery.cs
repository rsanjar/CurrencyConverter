using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRatesRange;

public record GetHistoricalRatesRangeQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    string BaseCurrency = "EUR",
    IEnumerable<string>? TargetCurrencies = null,
    int Page = 1,
    int PageSize = 20) : IQuery<HistoricalRatesPageResponse>;
