using System.Net;
using System.Text.Json;
using CurrencyConverter.IntegrationTests.Fixtures;
using FluentAssertions;

namespace CurrencyConverter.IntegrationTests.Controllers;

public class AuthControllerTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task PostToken_WithInvalidCredentials_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/token",
            new { Username = "anyone", Password = "anypass" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostToken_ReturnsNonEmptyToken()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/token",
            new { Username = "testuser", Password = "testpass" });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PostToken_ReturnsPositiveExpiresIn()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/token",
            new { Username = "testuser", Password = "testpass" });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostToken_TokenHasThreeJwtSegments()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/token",
            new { Username = "testuser", Password = "testpass" });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;

        token.Split('.').Should().HaveCount(3, "a JWT always consists of three dot-separated Base64 segments");
    }

    [Fact]
    public async Task PostToken_IssuedTokenAuthenticatesSubsequentRequests()
    {
        var tokenResponse = await Client.PostAsJsonAsync("/api/v1/auth/token",
            new { Username = "testuser", Password = "testpass" });
        var body = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;

        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        ExchangeRateProvider
            .GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro" });

        var currenciesResponse = await Client.GetAsync("/api/v1/currencies");

        currenciesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
