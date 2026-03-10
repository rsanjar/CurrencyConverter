using System.Text.Json;
using System.Text.Json.Serialization;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IFrankfurterService"/> that calls the public Frankfurter REST API.
/// API docs: https://www.frankfurter.app/docs/
/// </summary>
public class FrankfurterService(HttpClient httpClient) : IFrankfurterService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <inheritdoc/>
    public async Task<ExchangeRateData> GetLatestRatesAsync(
        string baseCurrency = "EUR",
        IEnumerable<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("latest", baseCurrency, targetCurrencies);
        return await FetchRatesAsync(url, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExchangeRateData> GetHistoricalRatesAsync(
        DateOnly date,
        string baseCurrency = "EUR",
        IEnumerable<string>? targetCurrencies = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(date.ToString("yyyy-MM-dd"), baseCurrency, targetCurrencies);
        return await FetchRatesAsync(url, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExchangeRateData> ConvertAsync(
        decimal amount,
        string fromCurrency,
        IEnumerable<string> toCurrencies,
        CancellationToken cancellationToken = default)
    {
        var toList = toCurrencies.ToList();
        var url = BuildUrl("latest", fromCurrency, toList, amount);
        return await FetchRatesAsync(url, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, string>> GetAvailableCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("currencies", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                     ?? new Dictionary<string, string>();

        return result;
    }

    private async Task<ExchangeRateData> FetchRatesAsync(string url, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var dto = JsonSerializer.Deserialize<FrankfurterResponseDto>(json, JsonOptions)
                  ?? throw new InvalidOperationException("Unexpected null response from Frankfurter API.");

        return new ExchangeRateData(
            dto.Amount,
            dto.Base,
            DateOnly.Parse(dto.Date),
            dto.Rates);
    }

    private static string BuildUrl(
        string endpoint,
        string baseCurrency,
        IEnumerable<string>? targetCurrencies,
        decimal? amount = null)
    {
        var query = new List<string>();

        if (!string.Equals(baseCurrency, "EUR", StringComparison.OrdinalIgnoreCase))
            query.Add($"from={Uri.EscapeDataString(baseCurrency.ToUpperInvariant())}");

        if (targetCurrencies != null)
        {
            var targets = string.Join(",", targetCurrencies.Select(c => Uri.EscapeDataString(c.ToUpperInvariant())));
            if (!string.IsNullOrEmpty(targets))
                query.Add($"to={targets}");
        }

        if (amount.HasValue)
            query.Add($"amount={amount.Value}");

        return query.Count > 0
            ? $"{endpoint}?{string.Join("&", query)}"
            : endpoint;
    }

    private sealed class FrankfurterResponseDto
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; init; }

        [JsonPropertyName("base")]
        public string Base { get; init; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; init; } = string.Empty;

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; init; } = new();
    }
}
