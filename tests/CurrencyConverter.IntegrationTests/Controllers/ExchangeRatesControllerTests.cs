using System.Net;
using System.Text.Json;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.IntegrationTests.Fixtures;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.IntegrationTests.Controllers;

public class ExchangeRatesControllerTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private static ExchangeRateData SampleRateData(string baseCurrency = "EUR") =>
        new(1m, baseCurrency, DateOnly.FromDateTime(DateTime.UtcNow),
            new Dictionary<string, decimal> { ["USD"] = 1.1m, ["GBP"] = 0.85m });

    private static HistoricalRatesRangeData SampleRangeData()
    {
        var start = new DateOnly(2023, 1, 2);
        return new HistoricalRatesRangeData(
            1m, "EUR", start, start.AddDays(1),
            new Dictionary<DateOnly, IReadOnlyDictionary<string, decimal>>
            {
                [start]             = new Dictionary<string, decimal> { ["USD"] = 1.10m },
                [start.AddDays(1)]  = new Dictionary<string, decimal> { ["USD"] = 1.11m },
            });
    }

    // ── Unauthenticated requests ──────────────────────────────────────────────

    [Theory]
    [InlineData("/api/exchangerates/conversion")]
    [InlineData("/api/exchangerates/by-date")]
    [InlineData("/api/exchangerates/amount-conversion")]
    [InlineData("/api/exchangerates/history")]
    public async Task AllEndpoints_WithoutAuth_Return401(string endpoint)
    {
        var response = await Client.PostAsJsonAsync(endpoint, new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/exchangerates/conversion ────────────────────────────────────

    [Fact]
    public async Task GetLatest_WithValidRequest_Returns200AndRates()
    {
        ExchangeRateProvider
            .GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRateData("USD"));
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/conversion",
            new { BaseCurrency = "USD" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("base").GetString().Should().Be("USD");
    }

    [Fact]
    public async Task GetLatest_WithInvalidBaseCurrency_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/conversion",
            new { BaseCurrency = "INVALID" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetLatest_WithNumericBaseCurrency_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/conversion",
            new { BaseCurrency = "1US" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /api/exchangerates/by-date ──────────────────────────────────────

    [Fact]
    public async Task GetHistorical_WithValidDate_Returns200()
    {
        ExchangeRateProvider
            .GetHistoricalRatesAsync(Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRateData());
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/by-date",
            new { Date = new DateOnly(2023, 6, 15), BaseCurrency = "EUR" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHistorical_WithFutureDate_Returns422()
    {
        await AuthenticateAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        var response = await Client.PostAsJsonAsync("/api/exchangerates/by-date",
            new { Date = futureDate, BaseCurrency = "EUR" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetHistorical_WithDateBeforeEarliest_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/by-date",
            new { Date = new DateOnly(1998, 12, 31), BaseCurrency = "EUR" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /api/exchangerates/amount-conversion ─────────────────────────────

    [Fact]
    public async Task Convert_WithValidRequest_Returns200()
    {
        ExchangeRateProvider
            .ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(SampleRateData());
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/amount-conversion",
            new { Amount = 100m, FromCurrency = "EUR", ToCurrencies = new[] { "GBP" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task Convert_WithRestrictedFromCurrency_Returns400(string restricted)
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/amount-conversion",
            new { Amount = 100m, FromCurrency = restricted, ToCurrencies = new[] { "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task Convert_WithRestrictedToCurrency_Returns400(string restricted)
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/amount-conversion",
            new { Amount = 100m, FromCurrency = "EUR", ToCurrencies = new[] { restricted } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Convert_WithZeroAmount_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/amount-conversion",
            new { Amount = 0m, FromCurrency = "EUR", ToCurrencies = new[] { "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Convert_WithNegativeAmount_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/amount-conversion",
            new { Amount = -50m, FromCurrency = "EUR", ToCurrencies = new[] { "USD" } });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Convert_WithEmptyToCurrencies_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/amount-conversion",
            new { Amount = 100m, FromCurrency = "EUR", ToCurrencies = Array.Empty<string>() });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /api/exchangerates/history ───────────────────────────────────────

    [Fact]
    public async Task GetHistory_WithValidRequest_Returns200WithPaginationFields()
    {
        ExchangeRateProvider
            .GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRangeData());
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/history",
            new { StartDate = new DateOnly(2023, 1, 2), EndDate = new DateOnly(2023, 1, 3), BaseCurrency = "GBP", Page = 1, PageSize = 10 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(2);
        body.GetProperty("totalPages").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetHistory_WithZeroPageSize_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/history",
            new { StartDate = new DateOnly(2023, 1, 2), EndDate = new DateOnly(2023, 1, 3), BaseCurrency = "EUR", Page = 1, PageSize = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetHistory_WithPageSizeOver100_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/history",
            new { StartDate = new DateOnly(2023, 1, 2), EndDate = new DateOnly(2023, 1, 3), BaseCurrency = "EUR", Page = 1, PageSize = 101 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetHistory_WithEndDateBeforeStartDate_Returns422()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/api/exchangerates/history",
            new { StartDate = new DateOnly(2023, 6, 1), EndDate = new DateOnly(2023, 5, 1), BaseCurrency = "EUR", Page = 1, PageSize = 20 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
