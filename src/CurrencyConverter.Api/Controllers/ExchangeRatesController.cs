using CurrencyConverter.Api.Configuration;
using CurrencyConverter.Api.Extensions;
using CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;
using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;
using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRatesRange;
using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CurrencyConverter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Authenticated)]
public class ExchangeRatesController(IMediator mediator) : ControllerBase
{
    [EndpointSummary("To target currencies")]
    [EndpointDescription("Converts from base currency to one or many target currencies")]
    [HttpPost("conversion")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLatest(GetLatestRatesQuery request)
    {
        var result = await mediator.Send(request, HttpContext.RequestAborted);

        return this.ToOkResult(result);
    }
    [EndpointSummary("By date")]
    [EndpointDescription("Gets historical exchange rates for a specific date.")]
    [HttpPost("by-date")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistorical(GetHistoricalRatesQuery request)
    {
        var result = await mediator.Send(request, HttpContext.RequestAborted);

        return this.ToOkResult(result);
    }

    [EndpointSummary("To target currencies with amount")]
    [EndpointDescription("Converts an amount from one currency to one or more target currencies.")]
    [HttpPost("amount-conversion")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Convert(ConvertCurrencyQuery request)
    {
        var result = await mediator.Send(request, HttpContext.RequestAborted);

        return this.ToOkResult(result);
    }

    [EndpointSummary("Historical rates")]
    [EndpointDescription("Gets historical exchange rates for a date range with pagination.")]
    [HttpPost("history")]
    [ProducesResponseType(typeof(HistoricalRatesPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetHistoricalRange(GetHistoricalRatesRangeQuery request)
    {
        var result = await mediator.Send(request, HttpContext.RequestAborted);

        return this.ToOkResult(result);
    }
}
