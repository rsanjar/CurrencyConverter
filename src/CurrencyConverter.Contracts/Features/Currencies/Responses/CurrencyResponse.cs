namespace CurrencyConverter.Contracts.Features.Currencies.Responses;

/// <summary>
/// Response model for a supported currency.
/// </summary>
public record CurrencyResponse(string Code, string Name) : IApiResponse;
