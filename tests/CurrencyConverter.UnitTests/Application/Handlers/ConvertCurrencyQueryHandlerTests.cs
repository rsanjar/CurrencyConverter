using CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;
using CurrencyConverter.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Handlers;

public class ConvertCurrencyQueryHandlerTests
{
    private readonly IExchangeRateProvider _provider = Substitute.For<IExchangeRateProvider>();
    private readonly IExchangeRateProviderFactory _factory = Substitute.For<IExchangeRateProviderFactory>();
    private readonly ConvertCurrencyQueryHandler _handler;

    public ConvertCurrencyQueryHandlerTests()
    {
        _factory.GetDefaultProvider().Returns(_provider);
        _handler = new ConvertCurrencyQueryHandler(_factory);
    }

    private static ExchangeRateData MakeConversionData(string from = "EUR") =>
        new(100m, from, DateOnly.FromDateTime(DateTime.UtcNow),
            new Dictionary<string, decimal> { ["USD"] = 110m, ["GBP"] = 86m });

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSuccess()
    {
        _provider.ConvertAsync(100m, "EUR", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(MakeConversionData());

        var result = await _handler.Handle(new ConvertCurrencyQuery(100m, "EUR", ["USD"]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task Handle_RestrictedFromCurrency_ReturnsFailure(string restricted)
    {
        var result = await _handler.Handle(new ConvertCurrencyQuery(100m, restricted, ["USD"]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task Handle_RestrictedToCurrency_ReturnsFailure(string restricted)
    {
        var result = await _handler.Handle(new ConvertCurrencyQuery(100m, "EUR", [restricted]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_RestrictedFromCurrency_ErrorListsAllRestrictedCurrencies()
    {
        var result = await _handler.Handle(new ConvertCurrencyQuery(100m, "TRY", ["USD"]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("MXN")
            .And.Contain("PLN")
            .And.Contain("THB")
            .And.Contain("TRY");
    }

    [Fact]
    public async Task Handle_MultipleRestrictedCurrencies_ErrorListsAll()
    {
        var result = await _handler.Handle(new ConvertCurrencyQuery(100m, "TRY", ["PLN"]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("TRY").And.Contain("PLN");
    }

    [Fact]
    public async Task Handle_RestrictedCurrency_DoesNotCallService()
    {
        await _handler.Handle(new ConvertCurrencyQuery(100m, "TRY", ["USD"]), CancellationToken.None);

        await _provider.DidNotReceive()
            .ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalizesFromCurrencyToUppercase()
    {
        _provider.ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(MakeConversionData("EUR"));

        await _handler.Handle(new ConvertCurrencyQuery(100m, "eur", ["gbp"]), CancellationToken.None);

        await _provider.Received(1).ConvertAsync(
            100m,
            "EUR",
            Arg.Is<IEnumerable<string>>(x => x.Contains("GBP")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalizesToCurrenciesToUppercase()
    {
        _provider.ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(MakeConversionData());

        await _handler.Handle(new ConvertCurrencyQuery(100m, "EUR", ["usd", "gbp"]), CancellationToken.None);

        await _provider.Received(1).ConvertAsync(
            100m,
            "EUR",
            Arg.Is<IEnumerable<string>>(x => x.Contains("USD") && x.Contains("GBP")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsConversionResponseCorrectly()
    {
        var date = new DateOnly(2024, 5, 1);
        var rates = new Dictionary<string, decimal> { ["GBP"] = 0.85m };
        _provider.ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateData(100m, "EUR", date, rates));

        var result = await _handler.Handle(new ConvertCurrencyQuery(100m, "EUR", ["GBP"]), CancellationToken.None);

        result.Data!.Amount.Should().Be(100m);
        result.Data.From.Should().Be("EUR");
        result.Data.Date.Should().Be(date);
        result.Data.Rates.Should().BeEquivalentTo(rates);
    }
}
