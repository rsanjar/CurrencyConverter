using Asp.Versioning;
using CurrencyConverter.Api.Configuration;
using CurrencyConverter.Api.Extensions;
using CurrencyConverter.Application.Features.Currencies.Queries.GetAvailableCurrencies;
using CurrencyConverter.Contracts.Features.Currencies.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CurrencyConverter.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Authenticated)]
public class CurrenciesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets all currencies supported by the Frankfurter API.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CurrencyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await mediator.Send(new GetAvailableCurrenciesQuery(), HttpContext.RequestAborted);
        return this.ToOkResult(result);
    }
}
