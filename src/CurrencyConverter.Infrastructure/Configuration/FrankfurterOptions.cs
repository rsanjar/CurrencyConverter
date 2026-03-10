namespace CurrencyConverter.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the Frankfurter API HTTP client.
/// </summary>
public class FrankfurterOptions
{
    public const string SectionName = "Frankfurter";

    /// <summary>
    /// Base URL of the Frankfurter API (default: https://api.frankfurter.app).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.frankfurter.app";

    /// <summary>
    /// HTTP request timeout in seconds (default: 30).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
