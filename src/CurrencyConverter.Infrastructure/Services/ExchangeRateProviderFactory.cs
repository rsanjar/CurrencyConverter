using CurrencyConverter.Domain.Enums;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Infrastructure.Services;

/// <summary>
/// Resolves <see cref="IExchangeRateProvider"/> implementations by type.
/// <para>
/// Adding a new provider requires only two steps:
/// <list type="number">
///   <item>Implement <see cref="IExchangeRateProvider"/> in Infrastructure.</item>
///   <item>Register the HTTP client and add a case to <see cref="GetProvider"/>.</item>
/// </list>
/// No changes are needed in the Application or Domain layers.
/// </para>
/// </summary>
public sealed class ExchangeRateProviderFactory(
    IServiceProvider serviceProvider,
    IOptions<ExchangeRateProviderOptions> options) : IExchangeRateProviderFactory
{
    /// <inheritdoc/>
    public IExchangeRateProvider GetProvider(ExchangeRateProviderType type) =>
        type switch
        {
            ExchangeRateProviderType.Frankfurter =>
                serviceProvider.GetRequiredService<FrankfurterService>(),

            _ => throw new NotSupportedException(
                $"No exchange-rate provider is registered for '{type}'. " +
                $"Implement {nameof(IExchangeRateProvider)}, register the HTTP client, " +
                $"and add a case to {nameof(ExchangeRateProviderFactory)}.{nameof(GetProvider)}.")
        };

    /// <inheritdoc/>
    public IExchangeRateProvider GetDefaultProvider() =>
        GetProvider(options.Value.DefaultProvider);
}
