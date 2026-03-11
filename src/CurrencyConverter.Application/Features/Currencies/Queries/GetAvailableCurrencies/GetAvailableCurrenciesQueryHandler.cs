using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Application.ResultResponse;
using CurrencyConverter.Contracts.Features.Currencies.Responses;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Application.Features.Currencies.Queries.GetAvailableCurrencies;

public class GetAvailableCurrenciesQueryHandler(IExchangeRateProviderFactory providerFactory)
    : IQueryHandler<GetAvailableCurrenciesQuery, IReadOnlyList<CurrencyResponse>>
{
    public async Task<Result<IReadOnlyList<CurrencyResponse>>> Handle(
        GetAvailableCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        var provider = providerFactory.GetDefaultProvider();

        var currencies = await provider.GetAvailableCurrenciesAsync(cancellationToken);

        var response = currencies
            .Select(kvp => new CurrencyResponse(kvp.Key, kvp.Value))
            .OrderBy(c => c.Code)
            .ToList();

        return Result<IReadOnlyList<CurrencyResponse>>.Success(response);
    }
}
