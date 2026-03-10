using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Application.ResultResponse;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;

public class GetLatestRatesQueryHandler(IFrankfurterService frankfurterService)
    : IQueryHandler<GetLatestRatesQuery, ExchangeRateResponse>
{
    public async Task<Result<ExchangeRateResponse>> Handle(
        GetLatestRatesQuery request,
        CancellationToken cancellationToken)
    {
        var data = await frankfurterService.GetLatestRatesAsync(
            request.BaseCurrency,
            request.TargetCurrencies,
            cancellationToken);

        var response = new ExchangeRateResponse(data.Amount, data.Base, data.Date, data.Rates);

        return Result<ExchangeRateResponse>.Success(response);
    }
}
