using CurrencyConverter.Application.Common.Security;
using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using TheTechLoop.HybridCache.MediatR.Abstractions;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;

/// <summary>
/// Query to get historical exchange rates for a specific date.
/// </summary>
public record GetHistoricalRatesQuery(
    DateOnly Date,
    string BaseCurrency = "EUR",
    IEnumerable<string>? TargetCurrencies = null) : IQuery<ExchangeRateResponse>, ICacheable, IAuthorizable
{
    string ICacheable.CacheKey =>
        $"ExchangeRates:Historical:{Date:yyyy-MM-dd}:{BaseCurrency}:{string.Join("-", (TargetCurrencies ?? Enumerable.Empty<string>()).OrderBy(x => x))}";

    TimeSpan ICacheable.CacheDuration => TimeSpan.FromHours(24);
}
