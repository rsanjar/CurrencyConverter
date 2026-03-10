using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Application.ResultResponse;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;

public class ConvertCurrencyQueryHandler(IFrankfurterService frankfurterService)
    : IQueryHandler<ConvertCurrencyQuery, ConversionResponse>
{
    public async Task<Result<ConversionResponse>> Handle(
        ConvertCurrencyQuery request,
        CancellationToken cancellationToken)
    {
        var data = await frankfurterService.ConvertAsync(
            request.Amount,
            request.FromCurrency,
            request.ToCurrencies,
            cancellationToken);

        var response = new ConversionResponse(data.Amount, data.Base, data.Date, data.Rates);

        return Result<ConversionResponse>.Success(response);
    }
}
