using CurrencyConverter.Application.Features.Currencies.Queries.GetAvailableCurrencies;
using CurrencyConverter.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Handlers;

public class GetAvailableCurrenciesQueryHandlerTests
{
    private readonly IExchangeRateProvider _provider = Substitute.For<IExchangeRateProvider>();
    private readonly IExchangeRateProviderFactory _factory = Substitute.For<IExchangeRateProviderFactory>();
    private readonly GetAvailableCurrenciesQueryHandler _handler;

    public GetAvailableCurrenciesQueryHandlerTests()
    {
        _factory.GetDefaultProvider().Returns(_provider);
        _handler = new GetAvailableCurrenciesQueryHandler(_factory);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSuccess()
    {
        _provider.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro" });

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MapsCodeAndNameCorrectly()
    {
        _provider.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro" });

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.Data!.Single().Code.Should().Be("EUR");
        result.Data.Single().Name.Should().Be("Euro");
    }

    [Fact]
    public async Task Handle_ReturnsCurrenciesOrderedByCodeAscending()
    {
        _provider.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>
            {
                ["USD"] = "US Dollar",
                ["EUR"] = "Euro",
                ["GBP"] = "British Pound",
                ["AUD"] = "Australian Dollar",
            });

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.Data!.Select(c => c.Code).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_EmptyDictionary_ReturnsSuccessWithEmptyList()
    {
        _provider.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ManyCurrencies_MapsAllEntries()
    {
        var currencies = new Dictionary<string, string>
        {
            ["EUR"] = "Euro",
            ["USD"] = "US Dollar",
            ["GBP"] = "British Pound",
            ["JPY"] = "Japanese Yen",
            ["CHF"] = "Swiss Franc",
        };

        _provider.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(currencies);

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.Data!.Should().HaveCount(5);
        result.Data.Select(c => c.Code).Should().BeEquivalentTo(currencies.Keys);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        _provider.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new GetAvailableCurrenciesQuery(), cts.Token);

        await _provider.Received(1).GetAvailableCurrenciesAsync(cts.Token);
    }
}
