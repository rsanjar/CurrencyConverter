using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;

/// <summary>
/// Query to convert an amount from one currency to one or more target currencies.
/// </summary>
public record ConvertCurrencyQuery(
    decimal Amount,
    string FromCurrency,
    IEnumerable<string> ToCurrencies) : IQuery<ConversionResponse>;
