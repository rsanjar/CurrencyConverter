using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;

/// <summary>
/// Query to get the latest exchange rates from the Frankfurter API.
/// </summary>
public record GetLatestRatesQuery(
    string BaseCurrency = "EUR",
    IEnumerable<string>? TargetCurrencies = null) : IQuery<ExchangeRateResponse>;
