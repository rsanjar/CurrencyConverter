using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Application.ResultResponse;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;

public class GetHistoricalRatesQueryHandler(IFrankfurterService frankfurterService)
    : IQueryHandler<GetHistoricalRatesQuery, ExchangeRateResponse>
{
    public async Task<Result<ExchangeRateResponse>> Handle(
        GetHistoricalRatesQuery request,
        CancellationToken cancellationToken)
    {
        var data = await frankfurterService.GetHistoricalRatesAsync(
            request.Date,
            request.BaseCurrency,
            request.TargetCurrencies,
            cancellationToken);

        var response = new ExchangeRateResponse(data.Amount, data.Base, data.Date, data.Rates);

        return Result<ExchangeRateResponse>.Success(response);
    }
}
