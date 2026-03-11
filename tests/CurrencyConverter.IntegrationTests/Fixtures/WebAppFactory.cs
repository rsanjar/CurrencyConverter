using CurrencyConverter.Domain.Enums;
using CurrencyConverter.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace CurrencyConverter.IntegrationTests.Fixtures;

/// <summary>
/// Shared test server factory. Replaces <see cref="IExchangeRateProviderFactory"/> with an
/// NSubstitute mock so integration tests never hit the real Frankfurter API.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    /// <summary>The mock provider that every test class in this fixture can configure and verify.</summary>
    public IExchangeRateProvider ExchangeRateProvider { get; } = Substitute.For<IExchangeRateProvider>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Provide known test credentials so the auth endpoint is enabled in the test server.
            // appsettings.Development.json may set different credentials; we override here to keep
            // tests self-contained and independent of local developer settings.
            // Caching and rate limiting are both disabled so every test gets a cold, isolated response.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:TestUsername"] = "testuser",
                ["JwtSettings:TestPassword"] = "testpass",
                ["TheTechLoopCache:Enabled"] = "false",
                ["RateLimiting:Enabled"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the real factory with a mock so tests control every provider response
            // without touching the network or real HTTP clients.
            services.RemoveAll<IExchangeRateProviderFactory>();

            var mockFactory = Substitute.For<IExchangeRateProviderFactory>();
            mockFactory.GetDefaultProvider().Returns(ExchangeRateProvider);
            mockFactory.GetProvider(Arg.Any<ExchangeRateProviderType>()).Returns(ExchangeRateProvider);

            services.AddSingleton(mockFactory);
        });
    }
}
