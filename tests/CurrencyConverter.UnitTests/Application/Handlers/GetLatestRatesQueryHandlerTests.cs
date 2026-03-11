using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;
using CurrencyConverter.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Handlers;

public class GetLatestRatesQueryHandlerTests
{
    private readonly IExchangeRateProvider _provider = Substitute.For<IExchangeRateProvider>();
    private readonly IExchangeRateProviderFactory _factory = Substitute.For<IExchangeRateProviderFactory>();
    private readonly GetLatestRatesQueryHandler _handler;

    public GetLatestRatesQueryHandlerTests()
    {
        _factory.GetDefaultProvider().Returns(_provider);
        _handler = new GetLatestRatesQueryHandler(_factory);
    }

    private static ExchangeRateData MakeData(string baseCurrency = "EUR") =>
        new(1m, baseCurrency, new DateOnly(2024, 1, 15),
            new Dictionary<string, decimal> { ["USD"] = 1.1m, ["GBP"] = 0.85m });

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSuccess()
    {
        _provider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData());

        var result = await _handler.Handle(new GetLatestRatesQuery("EUR"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MapsDataToResponse()
    {
        var date = new DateOnly(2024, 3, 20);
        var rates = new Dictionary<string, decimal> { ["JPY"] = 160m };
        _provider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateData(1m, "USD", date, rates));

        var result = await _handler.Handle(new GetLatestRatesQuery("USD"), CancellationToken.None);

        result.Data!.Base.Should().Be("USD");
        result.Data.Date.Should().Be(date);
        result.Data.Amount.Should().Be(1m);
        result.Data.Rates.Should().BeEquivalentTo(rates);
    }

    [Fact]
    public async Task Handle_PassesBaseCurrencyToService()
    {
        _provider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData("USD"));

        await _handler.Handle(new GetLatestRatesQuery("USD"), CancellationToken.None);

        await _provider.Received(1).GetLatestRatesAsync("USD", Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesTargetCurrenciesToService()
    {
        var targets = new[] { "USD", "GBP" };
        _provider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData());

        await _handler.Handle(new GetLatestRatesQuery("EUR", targets), CancellationToken.None);

        await _provider.Received(1).GetLatestRatesAsync("EUR", targets, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        _provider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData());
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new GetLatestRatesQuery("EUR"), cts.Token);

        await _provider.Received(1).GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), cts.Token);
    }

    [Fact]
    public async Task Handle_NullTargetCurrencies_PassesNullToService()
    {
        _provider.GetLatestRatesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData());

        await _handler.Handle(new GetLatestRatesQuery("EUR", null), CancellationToken.None);

        await _provider.Received(1).GetLatestRatesAsync("EUR", null, Arg.Any<CancellationToken>());
    }
}
