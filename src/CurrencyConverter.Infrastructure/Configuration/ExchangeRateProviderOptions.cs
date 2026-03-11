using CurrencyConverter.Domain.Enums;

namespace CurrencyConverter.Infrastructure.Configuration;

/// <summary>
/// Configuration for the active exchange-rate provider.
/// Bind to the <c>"ExchangeRateProvider"</c> section in appsettings.json.
/// </summary>
public sealed class ExchangeRateProviderOptions
{
    public const string SectionName = "ExchangeRateProvider";

    /// <summary>
    /// The provider used when a request does not specify one explicitly.
    /// Defaults to <see cref="ExchangeRateProviderType.Frankfurter"/>.
    /// </summary>
    public ExchangeRateProviderType DefaultProvider { get; set; } = ExchangeRateProviderType.Frankfurter;
}
