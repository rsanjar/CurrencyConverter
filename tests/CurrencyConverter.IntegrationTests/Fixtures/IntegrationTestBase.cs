using System.Net.Http.Headers;
using System.Text.Json;
using CurrencyConverter.Domain.Interfaces;

namespace CurrencyConverter.IntegrationTests.Fixtures;

/// <summary>
/// Base class for all integration tests. Each xUnit test class that inherits this
/// receives its own <see cref="HttpClient"/> (xUnit constructs a new instance per test
/// method), while the <see cref="WebAppFactory"/> is shared for the lifetime of the
/// test class via <see cref="IClassFixture{TFixture}"/>.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebAppFactory>
{
    protected readonly WebAppFactory Factory;
    protected readonly HttpClient Client;
    protected readonly IExchangeRateProvider ExchangeRateProvider;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected IntegrationTestBase(WebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        ExchangeRateProvider = factory.ExchangeRateProvider;
    }

    /// <summary>Obtains a JWT from the auth endpoint using the configured test credentials.</summary>
    protected async Task<string> GetAuthTokenAsync(string username = "testuser", string password = "testpass")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/token", new { Username = username, Password = password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("token").GetString()
               ?? throw new InvalidOperationException("Auth endpoint returned no token.");
    }

    /// <summary>Sets the Authorization header on the shared client with a freshly issued JWT.</summary>
    protected async Task AuthenticateAsync()
    {
        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
