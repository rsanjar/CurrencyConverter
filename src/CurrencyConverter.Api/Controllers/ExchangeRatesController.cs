using CurrencyConverter.Api.Extensions;
using CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;
using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;
using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;
using CurrencyConverter.Contracts.Features.ExchangeRates.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExchangeRatesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets the latest exchange rates.
    /// </summary>
    /// <param name="base">Base currency code (default: EUR).</param>
    /// <param name="to">Comma-separated list of target currency codes (optional).</param>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLatest(
        [FromQuery(Name = "base")] string baseCurrency = "EUR",
        [FromQuery(Name = "to")] string? to = null)
    {
        var targetCurrencies = ParseCurrencies(to);
        var result = await mediator.Send(
            new GetLatestRatesQuery(baseCurrency, targetCurrencies),
            HttpContext.RequestAborted);

        return this.ToOkResult(result);
    }

    /// <summary>
    /// Gets historical exchange rates for a specific date.
    /// </summary>
    /// <param name="date">The date in YYYY-MM-DD format.</param>
    /// <param name="base">Base currency code (default: EUR).</param>
    /// <param name="to">Comma-separated list of target currency codes (optional).</param>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistorical(
        DateOnly date,
        [FromQuery(Name = "base")] string baseCurrency = "EUR",
        [FromQuery(Name = "to")] string? to = null)
    {
        var targetCurrencies = ParseCurrencies(to);
        var result = await mediator.Send(
            new GetHistoricalRatesQuery(date, baseCurrency, targetCurrencies),
            HttpContext.RequestAborted);

        return this.ToOkResult(result);
    }

    /// <summary>
    /// Converts an amount from one currency to one or more target currencies.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="from">The source currency code.</param>
    /// <param name="to">Comma-separated list of target currency codes.</param>
    [HttpGet("convert")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Convert(
        [FromQuery] decimal amount,
        [FromQuery] string from,
        [FromQuery] string to)
    {
        var targetCurrencies = ParseCurrencies(to) ?? [];
        var result = await mediator.Send(
            new ConvertCurrencyQuery(amount, from, targetCurrencies),
            HttpContext.RequestAborted);

        return this.ToOkResult(result);
    }

    private static IEnumerable<string>? ParseCurrencies(string? to) =>
        string.IsNullOrWhiteSpace(to)
            ? null
            : to.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
