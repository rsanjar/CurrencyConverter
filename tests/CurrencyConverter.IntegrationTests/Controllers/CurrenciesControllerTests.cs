using System.Net;
using System.Text.Json;
using CurrencyConverter.IntegrationTests.Fixtures;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.IntegrationTests.Controllers;

public class CurrenciesControllerTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_WithoutAuth_Returns401()
    {
        var response = await Client.GetAsync("/api/currencies");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_Returns200()
    {
        ExchangeRateProvider
            .GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro", ["USD"] = "US Dollar" });
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/currencies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ReturnsCurrencyListWithCodeAndName()
    {
        ExchangeRateProvider
            .GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro" });
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/currencies");
        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        var eurEntry = body!.First(c => c.GetProperty("code").GetString() == "EUR");
        eurEntry.GetProperty("name").GetString().Should().Be("Euro");
    }

    [Fact]
    public async Task GetAll_ReturnsCurrenciesOrderedByCodeAscending()
    {
        ExchangeRateProvider
            .GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>
            {
                ["USD"] = "US Dollar",
                ["EUR"] = "Euro",
                ["GBP"] = "British Pound",
            });
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/currencies");
        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        var codes = body!.Select(c => c.GetProperty("code").GetString()).ToArray();
        codes.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAll_EmptyResponse_Returns200()  
    {
        ExchangeRateProvider
            .GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/currencies");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ReturnsManyEntries_Returns200()
    {
        var currencies = Enumerable.Range(1, 10)
            .ToDictionary(i => $"C{i:D2}", i => $"Currency {i}");
        ExchangeRateProvider
            .GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(currencies);
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/currencies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
