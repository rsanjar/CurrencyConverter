using CurrencyConverter.Application.Common.Security;
using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using TheTechLoop.HybridCache.MediatR.Abstractions;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;

/// <summary>
/// Query to get the latest exchange rates from the Frankfurter API.
/// </summary>
public record GetLatestRatesQuery(
    string BaseCurrency = "EUR",
    IEnumerable<string>? TargetCurrencies = null) : IQuery<ExchangeRateResponse>, ICacheable, IAuthorizable
{
    string ICacheable.CacheKey =>
        $"ExchangeRates:Latest:{BaseCurrency}:{string.Join("-", (TargetCurrencies ?? Enumerable.Empty<string>()).OrderBy(x => x))}";

    TimeSpan ICacheable.CacheDuration => TimeSpan.FromMinutes(5);
}
