using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Application.ResultResponse;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;

public class ConvertCurrencyQueryHandler(IExchangeRateProviderFactory providerFactory)
    : IQueryHandler<ConvertCurrencyQuery, ConversionResponse>
{
    // These currencies are restricted due to limitations in the Frankfurter API.
    // It's not included into the Fluent Validation rules because we want to return a single error message listing all restricted currencies in the request, instead of separate messages for each currency.
    //These values can also be saved in appsettings.json
    private static readonly HashSet<string> RestrictedCurrencies = ["TRY", "PLN", "THB", "MXN"];

    public async Task<Result<ConversionResponse>> Handle(
        ConvertCurrencyQuery request,
        CancellationToken cancellationToken)
    {
        var fromCurrency = request.FromCurrency.ToUpperInvariant();
        var toCurrencies = request.ToCurrencies
            .Select(currency => currency.ToUpperInvariant())
            .ToArray();

        var restrictedInRequest = new[] { fromCurrency }
            .Concat(toCurrencies)
            .Where(RestrictedCurrencies.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (restrictedInRequest.Length > 0)
        {
            var joined = string.Join(", ", RestrictedCurrencies.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            return Result<ConversionResponse>.Failure(
                $"Currency conversion is not supported for: {joined}.");
        }

        var provider = providerFactory.GetDefaultProvider();

        var data = await provider.ConvertAsync(
            request.Amount,
            fromCurrency,
            toCurrencies,
            cancellationToken);

        var response = new ConversionResponse(data.Amount, data.Base, data.Date, data.Rates);

        return Result<ConversionResponse>.Success(response);
    }
}
