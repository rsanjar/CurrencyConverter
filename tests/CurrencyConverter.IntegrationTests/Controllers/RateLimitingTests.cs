using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CurrencyConverter.Domain.Enums;
using CurrencyConverter.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace CurrencyConverter.IntegrationTests.Controllers;

/// <summary>
/// Standalone factory — inherits directly from <see cref="WebApplicationFactory{TEntryPoint}"/>
/// rather than the shared <see cref="Fixtures.WebAppFactory"/> so it fully owns the
/// configuration with no ordering ambiguity.
/// Rate limiting is enabled with a very low permit limit (2) so tests can trigger 429
/// responses with minimal requests.
/// </summary>
public sealed class RateLimitingWebAppFactory : WebApplicationFactory<Program>
{
    public IExchangeRateProvider ExchangeRateProvider { get; } = Substitute.For<IExchangeRateProvider>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Disable caching so every call reaches the mock provider.
                ["TheTechLoopCache:Enabled"] = "false",
                // Enable rate limiting with a tiny per-user window (2 req/min).
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:Authenticated:PermitLimit"] = "2",
                ["RateLimiting:Authenticated:WindowSeconds"] = "60",
                // Keep auth-endpoint limit high so any incidental auth calls don't interfere.
                ["RateLimiting:Auth:PermitLimit"] = "100",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
                // Keep global-IP ceiling high so it does not interfere with the
                // per-user tests; a dedicated factory tests the global limiter.
                ["RateLimiting:GlobalIp:PermitLimit"] = "100",
                ["RateLimiting:GlobalIp:WindowSeconds"] = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExchangeRateProviderFactory>();

            var mockFactory = Substitute.For<IExchangeRateProviderFactory>();
            mockFactory.GetDefaultProvider().Returns(ExchangeRateProvider);
            mockFactory.GetProvider(Arg.Any<ExchangeRateProviderType>()).Returns(ExchangeRateProvider);
            services.AddSingleton(mockFactory);
        });
    }
}

/// <summary>
/// Integration tests that verify rate-limiting behaviour for authenticated API endpoints.
/// <para>
/// JWT tokens are minted directly (without calling <c>/api/auth/token</c>) using a known
/// test signing key so each test can supply a <b>unique username</b>, giving it an
/// isolated rate-limit partition even though <see cref="RateLimitingWebAppFactory"/> is
/// shared across the test class.
/// </para>
/// </summary>
public class RateLimitingTests(RateLimitingWebAppFactory factory)
    : IClassFixture<RateLimitingWebAppFactory>
{
    // Matches appsettings.Development.json — read eagerly by JWT bearer setup
    // before WebApplicationFactory config overrides can take effect.
    internal const string TestJwtSecret   = "dev-only-secret-key-at-least-32-chars-long!!";
    internal const string TestJwtIssuer   = "CurrencyConverter";
    internal const string TestJwtAudience = "CurrencyConverter";

    private static readonly ExchangeRateData SampleRates = new(
        1m, "EUR", DateOnly.FromDateTime(DateTime.UtcNow),
        new Dictionary<string, decimal> { ["USD"] = 1.1m });

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>
    /// Creates a signed JWT for a brand-new unique username so every test call gets
    /// its own rate-limit partition — no auth endpoint is involved.
    /// </summary>
    private static string UniqueToken()
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var username    = $"rl-{Guid.NewGuid():N}";

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub,  Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "user"),
            ],
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpRequestMessage Post<T>(string url, T body, string token) =>
        new(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
        };

    private static HttpRequestMessage Get(string url, string token) =>
        new(HttpMethod.Get, url)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
        };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthenticatedEndpoint_AfterExceedingPermitLimit_Returns429()
    {
        factory.ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRates);

        var client = NewClient();
        var token  = UniqueToken();
        var body   = new { BaseCurrency = "EUR" };

        // PermitLimit = 2 → first two succeed, third is rejected.
        var r1 = await client.SendAsync(Post("/api/exchangerates/conversion", body, token));
        var r2 = await client.SendAsync(Post("/api/exchangerates/conversion", body, token));
        var r3 = await client.SendAsync(Post("/api/exchangerates/conversion", body, token));

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        r3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimitedResponse_IncludesRetryAfterHeader()
    {
        factory.ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRates);

        var client = NewClient();
        var token  = UniqueToken();
        var body   = new { BaseCurrency = "EUR" };

        for (var i = 0; i < 2; i++)
            await client.SendAsync(Post("/api/exchangerates/conversion", body, token));

        var rejected = await client.SendAsync(Post("/api/exchangerates/conversion", body, token));

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Should().ContainKey("Retry-After");
        int.Parse(rejected.Headers.GetValues("Retry-After").First()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RateLimitedResponse_BodyContainsErrorMessage()
    {
        factory.ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRates);

        var client = NewClient();
        var token  = UniqueToken();
        var body   = new { BaseCurrency = "EUR" };

        for (var i = 0; i < 2; i++)
            await client.SendAsync(Post("/api/exchangerates/conversion", body, token));

        var rejected = await client.SendAsync(Post("/api/exchangerates/conversion", body, token));
        var json     = await rejected.Content.ReadFromJsonAsync<JsonElement>();

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        json.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DifferentUsers_HaveIndependentRateLimitBuckets()
    {
        factory.ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRates);

        var client = NewClient();
        var token1 = UniqueToken();
        var token2 = UniqueToken();
        var body   = new { BaseCurrency = "EUR" };

        // Exhaust user-1's budget.
        for (var i = 0; i < 2; i++)
            await client.SendAsync(Post("/api/exchangerates/conversion", body, token1));

        var user1Rejected = await client.SendAsync(Post("/api/exchangerates/conversion", body, token1));
        var user2Allowed  = await client.SendAsync(Post("/api/exchangerates/conversion", body, token2));

        user1Rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "user-1 has exhausted its rate-limit window");
        user2Allowed.StatusCode.Should().Be(HttpStatusCode.OK,
            "user-2 has an independent budget and has not exceeded it");
    }

    [Fact]
    public async Task MultipleEndpoints_ShareTheSamePerUserBudget()
    {
        factory.ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRates);
        factory.ExchangeRateProvider
            .GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro" });

        var client = NewClient();
        var token  = UniqueToken();

        // Spend first two permits across different endpoints.
        var r1 = await client.SendAsync(Get("/api/currencies", token));
        var r2 = await client.SendAsync(Post("/api/exchangerates/conversion",
            new { BaseCurrency = "EUR" }, token));

        // Third request (any endpoint) must now be rejected.
        var r3 = await client.SendAsync(Post("/api/exchangerates/conversion",
            new { BaseCurrency = "EUR" }, token));

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        r3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}

/// <summary>
/// Factory used exclusively by <see cref="GlobalIpRateLimitingTests"/>.
/// Sets a very low <c>GlobalIp</c> limit (3 req/min) and a high per-user limit so
/// only the global ceiling is being exercised.
/// </summary>
public sealed class GlobalIpRateLimitingWebAppFactory : WebApplicationFactory<Program>
{
    public IExchangeRateProvider ExchangeRateProvider { get; } = Substitute.For<IExchangeRateProvider>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TheTechLoopCache:Enabled"] = "false",
                ["RateLimiting:Enabled"] = "true",
                // Per-user limit is intentionally high so it never triggers —
                // only the global ceiling is under test here.
                ["RateLimiting:Authenticated:PermitLimit"] = "100",
                ["RateLimiting:Authenticated:WindowSeconds"] = "60",
                ["RateLimiting:Auth:PermitLimit"] = "100",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
                // Low global-IP ceiling: 3 total requests per window.
                ["RateLimiting:GlobalIp:PermitLimit"] = "3",
                ["RateLimiting:GlobalIp:WindowSeconds"] = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExchangeRateProviderFactory>();

            var mockFactory = Substitute.For<IExchangeRateProviderFactory>();
            mockFactory.GetDefaultProvider().Returns(ExchangeRateProvider);
            mockFactory.GetProvider(Arg.Any<ExchangeRateProviderType>()).Returns(ExchangeRateProvider);
            services.AddSingleton(mockFactory);
        });
    }
}

/// <summary>
/// Tests for the <c>GlobalLimiter</c> — the per-IP ceiling that is evaluated
/// before any named policy and caps total throughput from a single IP address
/// regardless of how many different user accounts are involved.
/// <para>
/// In the test server all connections have <c>RemoteIpAddress == null</c>, so every
/// request falls into the same <c>"unknown"</c> partition — a perfect stand-in for
/// "all traffic from one IP".
/// </para>
/// </summary>
public class GlobalIpRateLimitingTests(GlobalIpRateLimitingWebAppFactory factory)
    : IClassFixture<GlobalIpRateLimitingWebAppFactory>
{
    // Same signing key as RateLimitingTests (matches appsettings.Development.json).
    private const string Secret   = RateLimitingTests.TestJwtSecret;
    private const string Issuer   = RateLimitingTests.TestJwtIssuer;
    private const string Audience = RateLimitingTests.TestJwtAudience;

    private static readonly ExchangeRateData SampleRates = new(
        1m, "EUR", DateOnly.FromDateTime(DateTime.UtcNow),
        new Dictionary<string, decimal> { ["USD"] = 1.1m });

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>
    /// Creates a token for a brand-new unique username.
    /// Each call returns a token with a different <c>name</c> claim so every token
    /// represents a distinct user — yet they all share the same IP bucket.
    /// </summary>
    private static string UniqueToken()
    {
        var key      = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds    = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var username = $"gip-{Guid.NewGuid():N}";

        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub,  Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "user"),
            ],
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static HttpRequestMessage Post<T>(string url, T body, string token) =>
        new(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
        };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GlobalIpLimit_BlocksFourthRequest_EvenFromNewUser()
    {
        factory.ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRates);

        var client = NewClient();
        var body   = new { BaseCurrency = "EUR" };

        // GlobalIp PermitLimit = 3 → each with a distinct token (different users).
        var r1 = await client.SendAsync(Post("/api/exchangerates/conversion", body, UniqueToken()));
        var r2 = await client.SendAsync(Post("/api/exchangerates/conversion", body, UniqueToken()));
        var r3 = await client.SendAsync(Post("/api/exchangerates/conversion", body, UniqueToken()));
        // 4th user — would be within the per-user limit, but the IP ceiling is exhausted.
        var r4 = await client.SendAsync(Post("/api/exchangerates/conversion", body, UniqueToken()));

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        r3.StatusCode.Should().Be(HttpStatusCode.OK);
        r4.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "the global per-IP ceiling is exhausted even though each request used a different user account");
    }

    [Fact]
    public async Task GlobalIpLimit_RejectedResponse_IncludesRetryAfterHeader()
    {
        factory.ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRates);

        var client = NewClient();
        var body   = new { BaseCurrency = "EUR" };

        // Exhaust the 3-request global window.
        for (var i = 0; i < 3; i++)
            await client.SendAsync(Post("/api/exchangerates/conversion", body, UniqueToken()));

        var rejected = await client.SendAsync(Post("/api/exchangerates/conversion", body, UniqueToken()));

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Should().ContainKey("Retry-After");
        int.Parse(rejected.Headers.GetValues("Retry-After").First()).Should().BeGreaterThan(0);
    }
}
