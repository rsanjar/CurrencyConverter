using System.Net;
using System.Text;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Infrastructure.Services;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Infrastructure.Services;

/// <summary>
/// Captures the outgoing request and returns a configured response.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpMessageHandler(string jsonBody, HttpStatusCode status = HttpStatusCode.OK)
        : this(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        })
    { }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_respond(request));
    }
}

public class FrankfurterServiceTests
{
    private const string BaseUrl = "https://api.frankfurter.app/";

    private static (FrankfurterService Service, FakeHttpMessageHandler Handler) Create(
        string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(json, status);
        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        return (new FrankfurterService(client), handler);
    }

    // ── JSON templates ────────────────────────────────────────────────────────

    private static string RatesJson(string baseCurrency = "EUR", decimal usdRate = 1.1m) => $$"""
        {
            "amount": 1.0,
            "base": "{{baseCurrency}}",
            "date": "2024-01-15",
            "rates": { "USD": {{usdRate}} }
        }
        """;

    private static string HistoricalRangeJson() => """
        {
            "amount": 1.0,
            "base": "EUR",
            "start_date": "2024-01-01",
            "end_date": "2024-01-03",
            "rates": {
                "2024-01-01": { "USD": 1.10 },
                "2024-01-02": { "USD": 1.11 },
                "2024-01-03": { "USD": 1.12 }
            }
        }
        """;

    // ── GetLatestRatesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetLatestRatesAsync_EurBase_OmitsFromParam()
    {
        var (svc, handler) = Create(RatesJson());

        await svc.GetLatestRatesAsync("EUR");

        handler.LastRequest!.RequestUri!.Query.Should().NotContain("from=");
    }

    [Fact]
    public async Task GetLatestRatesAsync_NonEurBase_IncludesFromParam()
    {
        var (svc, handler) = Create(RatesJson("USD"));

        await svc.GetLatestRatesAsync("USD");

        handler.LastRequest!.RequestUri!.Query.Should().Contain("from=USD");
    }

    [Fact]
    public async Task GetLatestRatesAsync_WithTargetCurrencies_IncludesToParam()
    {
        var (svc, handler) = Create(RatesJson());

        await svc.GetLatestRatesAsync("EUR", ["USD", "GBP"]);

        handler.LastRequest!.RequestUri!.Query.Should().Contain("to=");
    }

    [Fact]
    public async Task GetLatestRatesAsync_RequestTargetsLatestEndpoint()
    {
        var (svc, handler) = Create(RatesJson());

        await svc.GetLatestRatesAsync("EUR");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("latest");
    }

    [Fact]
    public async Task GetLatestRatesAsync_ParsesResponseCorrectly()
    {
        var (svc, _) = Create(RatesJson("USD", 1.25m));

        var result = await svc.GetLatestRatesAsync("USD");

        result.Base.Should().Be("USD");
        result.Date.Should().Be(new DateOnly(2024, 1, 15));
        result.Rates["USD"].Should().Be(1.25m);
        result.Amount.Should().Be(1m);
    }

    // ── GetHistoricalRatesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetHistoricalRatesAsync_IncludesDateInRequestPath()
    {
        var (svc, handler) = Create(RatesJson());

        await svc.GetHistoricalRatesAsync(new DateOnly(2023, 6, 15), "EUR");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("2023-06-15");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_NonEurBase_IncludesFromParam()
    {
        var (svc, handler) = Create(RatesJson("GBP"));

        await svc.GetHistoricalRatesAsync(new DateOnly(2023, 6, 15), "GBP");

        handler.LastRequest!.RequestUri!.Query.Should().Contain("from=GBP");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ParsesResponseCorrectly()
    {
        var (svc, _) = Create(RatesJson("EUR", 1.08m));

        var result = await svc.GetHistoricalRatesAsync(new DateOnly(2024, 1, 15), "EUR");

        result.Base.Should().Be("EUR");
        result.Date.Should().Be(new DateOnly(2024, 1, 15));
    }

    // ── GetHistoricalRatesRangeAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetHistoricalRatesRangeAsync_IncludesDateRangeInPath()
    {
        var (svc, handler) = Create(HistoricalRangeJson());

        await svc.GetHistoricalRatesRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 3), "EUR");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("2024-01-01..2024-01-03");
    }

    [Fact]
    public async Task GetHistoricalRatesRangeAsync_ParsesRatesByDateCorrectly()
    {
        var (svc, _) = Create(HistoricalRangeJson());

        var result = await svc.GetHistoricalRatesRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 3));

        result.RatesByDate.Should().HaveCount(3);
        result.StartDate.Should().Be(new DateOnly(2024, 1, 1));
        result.EndDate.Should().Be(new DateOnly(2024, 1, 3));
        result.Base.Should().Be("EUR");
    }

    [Fact]
    public async Task GetHistoricalRatesRangeAsync_RatesByDateContainsExpectedValues()
    {
        var (svc, _) = Create(HistoricalRangeJson());

        var result = await svc.GetHistoricalRatesRangeAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 3));

        result.RatesByDate[new DateOnly(2024, 1, 1)]["USD"].Should().Be(1.10m);
        result.RatesByDate[new DateOnly(2024, 1, 3)]["USD"].Should().Be(1.12m);
    }

    // ── ConvertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_IncludesAmountInQuery()
    {
        var (svc, handler) = Create(RatesJson());

        await svc.ConvertAsync(250m, "EUR", ["USD"]);

        handler.LastRequest!.RequestUri!.Query.Should().Contain("amount=250");
    }

    [Fact]
    public async Task ConvertAsync_IncludesToParam()
    {
        var (svc, handler) = Create(RatesJson());

        await svc.ConvertAsync(100m, "EUR", ["GBP"]);

        handler.LastRequest!.RequestUri!.Query.Should().Contain("to=GBP");
    }

    [Fact]
    public async Task ConvertAsync_NonEurFrom_IncludesFromParam()
    {
        var (svc, handler) = Create(RatesJson("USD"));

        await svc.ConvertAsync(100m, "USD", ["GBP"]);

        handler.LastRequest!.RequestUri!.Query.Should().Contain("from=USD");
    }

    // ── GetAvailableCurrenciesAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetAvailableCurrenciesAsync_CallsCurrenciesEndpoint()
    {
        var (svc, handler) = Create("""{"EUR": "Euro"}""");

        await svc.GetAvailableCurrenciesAsync();

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("currencies");
    }

    [Fact]
    public async Task GetAvailableCurrenciesAsync_ParsesDictionaryCorrectly()
    {
        var (svc, _) = Create("""{"EUR": "Euro", "USD": "US Dollar"}""");

        var result = await svc.GetAvailableCurrenciesAsync();

        result.Should().ContainKey("EUR").WhoseValue.Should().Be("Euro");
        result.Should().ContainKey("USD").WhoseValue.Should().Be("US Dollar");
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAvailableCurrenciesAsync_EmptyResponse_ReturnsEmptyDictionary()
    {
        var (svc, _) = Create("{}");

        var result = await svc.GetAvailableCurrenciesAsync();

        result.Should().BeEmpty();
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLatestRatesAsync_HttpError_ThrowsHttpRequestException()
    {
        var (svc, _) = Create("", HttpStatusCode.InternalServerError);

        var act = () => svc.GetLatestRatesAsync("EUR");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_HttpError_ThrowsHttpRequestException()
    {
        var (svc, _) = Create("", HttpStatusCode.ServiceUnavailable);

        var act = () => svc.GetHistoricalRatesAsync(new DateOnly(2023, 1, 1), "EUR");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetAvailableCurrenciesAsync_HttpError_ThrowsHttpRequestException()
    {
        var (svc, _) = Create("", HttpStatusCode.BadGateway);

        var act = () => svc.GetAvailableCurrenciesAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetLatestRatesAsync_TargetCurrenciesAreUppercasedInUrl()
    {
        var (svc, handler) = Create(RatesJson());

        await svc.GetLatestRatesAsync("EUR", ["usd", "gbp"]);

        var query = handler.LastRequest!.RequestUri!.Query;
        // BuildUrl uppercases all currency codes before passing to URI
        query.Should().Contain("to=").And.Contain("USD").And.Contain("GBP");
    }
}
