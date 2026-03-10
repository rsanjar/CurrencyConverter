using CurrencyConverter.Application.Messaging;
using CurrencyConverter.Application.ResultResponse;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRatesRange;

public class GetHistoricalRatesRangeQueryHandler(IFrankfurterService frankfurterService)
    : IQueryHandler<GetHistoricalRatesRangeQuery, HistoricalRatesPageResponse>
{
    public async Task<Result<HistoricalRatesPageResponse>> Handle(
        GetHistoricalRatesRangeQuery request,
        CancellationToken cancellationToken)
    {
        var data = await frankfurterService.GetHistoricalRatesRangeAsync(
            request.StartDate,
            request.EndDate,
            request.BaseCurrency,
            request.TargetCurrencies,
            cancellationToken);

        var orderedRates = data.RatesByDate
            .OrderBy(entry => entry.Key)
            .ToArray();

        var totalCount = orderedRates.Length;
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var skip = (request.Page - 1) * request.PageSize;
        var pageItems = orderedRates
            .Skip(skip)
            .Take(request.PageSize)
            .Select(entry => new HistoricalRateItemResponse(entry.Key, entry.Value))
            .ToArray();

        var response = new HistoricalRatesPageResponse(
            data.Base,
            data.StartDate,
            data.EndDate,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages,
            pageItems);

        return Result<HistoricalRatesPageResponse>.Success(response);
    }
}
