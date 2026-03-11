using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;
using CurrencyConverter.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Handlers;

public class GetHistoricalRatesQueryHandlerTests
{
    private readonly IFrankfurterService _service = Substitute.For<IFrankfurterService>();
    private readonly GetHistoricalRatesQueryHandler _handler;

    public GetHistoricalRatesQueryHandlerTests()
    {
        _handler = new GetHistoricalRatesQueryHandler(_service);
    }

    private static ExchangeRateData MakeData(DateOnly date, string baseCurrency = "EUR") =>
        new(1m, baseCurrency, date, new Dictionary<string, decimal> { ["USD"] = 1.08m });

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSuccess()
    {
        var date = new DateOnly(2023, 6, 15);
        _service.GetHistoricalRatesAsync(date, "EUR", Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData(date));

        var result = await _handler.Handle(new GetHistoricalRatesQuery(date, "EUR"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MapsDataToResponse()
    {
        var date = new DateOnly(2023, 6, 15);
        _service.GetHistoricalRatesAsync(Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData(date, "USD"));

        var result = await _handler.Handle(new GetHistoricalRatesQuery(date, "USD"), CancellationToken.None);

        result.Data!.Date.Should().Be(date);
        result.Data.Base.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_PassesCorrectDateToService()
    {
        var date = new DateOnly(2020, 3, 15);
        _service.GetHistoricalRatesAsync(Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData(date));

        await _handler.Handle(new GetHistoricalRatesQuery(date, "EUR"), CancellationToken.None);

        await _service.Received(1).GetHistoricalRatesAsync(date, "EUR", Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesTargetCurrenciesToService()
    {
        var date = new DateOnly(2022, 5, 10);
        var targets = new[] { "USD", "GBP" };
        _service.GetHistoricalRatesAsync(Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData(date));

        await _handler.Handle(new GetHistoricalRatesQuery(date, "EUR", targets), CancellationToken.None);

        await _service.Received(1).GetHistoricalRatesAsync(date, "EUR", targets, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        var date = new DateOnly(2023, 1, 1);
        _service.GetHistoricalRatesAsync(Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(MakeData(date));
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new GetHistoricalRatesQuery(date, "EUR"), cts.Token);

        await _service.Received(1).GetHistoricalRatesAsync(Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), cts.Token);
    }
}
