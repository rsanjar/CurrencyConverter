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
            // Clear TestUsername so the auth endpoint accepts any credentials in tests.
            // appsettings.Development.json sets real dev credentials which would break tests.
            // Also disable caching so tests are fully isolated from each other.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:TestUsername"] = string.Empty,
                ["JwtSettings:TestPassword"] = string.Empty,
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
