using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Infrastructure.Configuration;
using CurrencyConverter.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(FrankfurterOptions.SectionName)
            .Get<FrankfurterOptions>() ?? new FrankfurterOptions();

        // ── HTTP Clients ─────────────────────────────────────────────────
        services.AddHttpClient<IFrankfurterService, FrankfurterService>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}
