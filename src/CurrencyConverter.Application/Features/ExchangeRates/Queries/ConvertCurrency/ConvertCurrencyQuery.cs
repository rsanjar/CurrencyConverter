using CurrencyConverter.Application.Common.Security;
using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using TheTechLoop.HybridCache.MediatR.Abstractions;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;

/// <summary>
/// Query to convert an amount from one currency to one or more target currencies.
/// </summary>
public record ConvertCurrencyQuery(
    decimal Amount,
    string FromCurrency,
    IEnumerable<string> ToCurrencies) : IQuery<ConversionResponse>, ICacheable, IAuthorizable
{
    string ICacheable.CacheKey =>
        $"ExchangeRates:Convert:{Amount}:{FromCurrency}:{string.Join("-", ToCurrencies.OrderBy(x => x))}";

    TimeSpan ICacheable.CacheDuration => TimeSpan.FromMinutes(5);
}
