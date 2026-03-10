using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;

/// <summary>
/// Query to get historical exchange rates for a specific date.
/// </summary>
public record GetHistoricalRatesQuery(
    DateOnly Date,
    string BaseCurrency = "EUR",
    IEnumerable<string>? TargetCurrencies = null) : IQuery<ExchangeRateResponse>;
