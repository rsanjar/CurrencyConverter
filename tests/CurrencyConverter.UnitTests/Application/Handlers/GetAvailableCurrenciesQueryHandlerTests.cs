using CurrencyConverter.Application.Features.Currencies.Queries.GetAvailableCurrencies;
using CurrencyConverter.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Handlers;

public class GetAvailableCurrenciesQueryHandlerTests
{
    private readonly IFrankfurterService _service = Substitute.For<IFrankfurterService>();
    private readonly GetAvailableCurrenciesQueryHandler _handler;

    public GetAvailableCurrenciesQueryHandlerTests()
    {
        _handler = new GetAvailableCurrenciesQueryHandler(_service);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSuccess()
    {
        _service.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro" });

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MapsCodeAndNameCorrectly()
    {
        _service.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["EUR"] = "Euro" });

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.Data!.Single().Code.Should().Be("EUR");
        result.Data.Single().Name.Should().Be("Euro");
    }

    [Fact]
    public async Task Handle_ReturnsCurrenciesOrderedByCodeAscending()
    {
        _service.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
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
        _service.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
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

        _service.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(currencies);

        var result = await _handler.Handle(new GetAvailableCurrenciesQuery(), CancellationToken.None);

        result.Data!.Should().HaveCount(5);
        result.Data.Select(c => c.Code).Should().BeEquivalentTo(currencies.Keys);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        _service.GetAvailableCurrenciesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new GetAvailableCurrenciesQuery(), cts.Token);

        await _service.Received(1).GetAvailableCurrenciesAsync(cts.Token);
    }
}
