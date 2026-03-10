using CurrencyConverter.Application.Common.Security;
using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using TheTechLoop.HybridCache.MediatR.Abstractions;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRatesRange;

public record GetHistoricalRatesRangeQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    string BaseCurrency = "EUR",
    IEnumerable<string>? TargetCurrencies = null,
    int Page = 1,
    int PageSize = 20) : IQuery<HistoricalRatesPageResponse>, ICacheable, IAuthorizable
{
    string ICacheable.CacheKey =>
        $"ExchangeRates:HistoricalRange:{StartDate:yyyy-MM-dd}:{EndDate:yyyy-MM-dd}:{BaseCurrency}:{string.Join("-", (TargetCurrencies ?? Enumerable.Empty<string>()).OrderBy(x => x))}:{Page}:{PageSize}";

    TimeSpan ICacheable.CacheDuration => TimeSpan.FromHours(24);
}
