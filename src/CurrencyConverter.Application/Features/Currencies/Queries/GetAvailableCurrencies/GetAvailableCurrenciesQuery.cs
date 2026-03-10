using CurrencyConverter.Application.Common.Security;
using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.Currencies.Responses;
using TheTechLoop.HybridCache.MediatR.Abstractions;

namespace CurrencyConverter.Application.Features.Currencies.Queries.GetAvailableCurrencies;

/// <summary>
/// Query to retrieve all currencies supported by the Frankfurter API.
/// </summary>
public record GetAvailableCurrenciesQuery : IQuery<IReadOnlyList<CurrencyResponse>>, ICacheable //IAuthorizable
{
    string ICacheable.CacheKey => "ExchangeRates:Currencies";
    TimeSpan ICacheable.CacheDuration => TimeSpan.FromMinutes(60);
}
