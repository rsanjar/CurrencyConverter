using CurrencyConverter.Domain.Enums;

namespace CurrencyConverter.Domain.Interfaces;

/// <summary>
/// Resolves the correct <see cref="IExchangeRateProvider"/> implementation at runtime.
/// <para>
/// Handlers call <see cref="GetDefaultProvider"/> to use the provider configured in
/// <c>appsettings.json</c>. When a request explicitly specifies a provider (e.g., from
/// a query parameter), call <see cref="GetProvider"/> with the desired
/// <see cref="ExchangeRateProviderType"/>.
/// </para>
/// <para>
/// To add a new provider: implement <see cref="IExchangeRateProvider"/>, register the
/// implementation in Infrastructure's DI setup, and add the corresponding case to
/// <c>ExchangeRateProviderFactory</c>.
/// </para>
/// </summary>
public interface IExchangeRateProviderFactory
{
    /// <summary>
    /// Returns the provider registered for <paramref name="type"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when no implementation is registered for the requested type.</exception>
    IExchangeRateProvider GetProvider(ExchangeRateProviderType type);

    /// <summary>
    /// Returns the provider that matches the default type configured in
    /// <c>ExchangeRateProvider:DefaultProvider</c> (appsettings.json).
    /// </summary>
    IExchangeRateProvider GetDefaultProvider();
}
