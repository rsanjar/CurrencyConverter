using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Contracts.Features.Currencies.Responses;

namespace CurrencyConverter.Application.Features.Currencies.Queries.GetAvailableCurrencies;

/// <summary>
/// Query to retrieve all currencies supported by the Frankfurter API.
/// </summary>
public record GetAvailableCurrenciesQuery : IQuery<IReadOnlyList<CurrencyResponse>>;
