using CurrencyConverter.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace CurrencyConverter.IntegrationTests.Fixtures;

/// <summary>
/// Shared test server factory. Replaces <see cref="IFrankfurterService"/> with an
/// NSubstitute mock so integration tests never hit the real Frankfurter API.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    /// <summary>The mock that every test class in this fixture can configure and verify.</summary>
    public IFrankfurterService FrankfurterService { get; } = Substitute.For<IFrankfurterService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Provide known test credentials so the auth endpoint is enabled in the test server.
            // appsettings.Development.json may set different credentials; we override here to keep
            // tests self-contained and independent of local developer settings.
            // Caching is disabled so every test gets a cold, isolated response.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:TestUsername"] = "testuser",
                ["JwtSettings:TestPassword"] = "testpass",
                ["TheTechLoopCache:Enabled"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the typed-client registration produced by AddHttpClient<IFrankfurterService, …>
            services.RemoveAll<IFrankfurterService>();

            // Register our mock as a singleton so every controller resolution uses it.
            services.AddSingleton(_ => FrankfurterService);
        });
    }
}
