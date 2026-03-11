using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRatesRange;
using CurrencyConverter.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Handlers;

public class GetHistoricalRatesRangeQueryHandlerTests
{
    private readonly IFrankfurterService _service = Substitute.For<IFrankfurterService>();
    private readonly GetHistoricalRatesRangeQueryHandler _handler;

    public GetHistoricalRatesRangeQueryHandlerTests()
    {
        _handler = new GetHistoricalRatesRangeQueryHandler(_service);
    }

    private static HistoricalRatesRangeData BuildRangeData(int dayCount = 5)
    {
        var startDate = new DateOnly(2023, 1, 2);
        var endDate = startDate.AddDays(dayCount - 1);
        var ratesByDate = Enumerable.Range(0, dayCount)
            .ToDictionary(
                i => startDate.AddDays(i),
                i => (IReadOnlyDictionary<string, decimal>)new Dictionary<string, decimal> { ["USD"] = 1.1m + i * 0.01m });

        return new HistoricalRatesRangeData(1m, "EUR", startDate, endDate, ratesByDate);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSuccess()
    {
        var data = BuildRangeData(3);
        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var result = await _handler.Handle(new GetHistoricalRatesRangeQuery(data.StartDate, data.EndDate, "EUR"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Page1_ReturnsFirstPageItems()
    {
        var data = BuildRangeData(5);
        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var result = await _handler.Handle(
            new GetHistoricalRatesRangeQuery(data.StartDate, data.EndDate, "EUR", Page: 1, PageSize: 3),
            CancellationToken.None);

        result.Data!.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(5);
        result.Data.TotalPages.Should().Be(2);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(3);
    }

    [Fact]
    public async Task Handle_LastPage_ReturnsRemainingItems()
    {
        var data = BuildRangeData(5);
        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var result = await _handler.Handle(
            new GetHistoricalRatesRangeQuery(data.StartDate, data.EndDate, "EUR", Page: 2, PageSize: 3),
            CancellationToken.None);

        result.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_PageBeyondTotal_ReturnsEmptyItems()
    {
        var data = BuildRangeData(3);
        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var result = await _handler.Handle(
            new GetHistoricalRatesRangeQuery(data.StartDate, data.EndDate, "EUR", Page: 99, PageSize: 20),
            CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyData_ReturnsZeroTotalPagesAndCount()
    {
        var emptyData = new HistoricalRatesRangeData(
            1m, "EUR",
            new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 1),
            new Dictionary<DateOnly, IReadOnlyDictionary<string, decimal>>());

        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(emptyData);

        var result = await _handler.Handle(
            new GetHistoricalRatesRangeQuery(emptyData.StartDate, emptyData.EndDate),
            CancellationToken.None);

        result.Data!.TotalPages.Should().Be(0);
        result.Data.TotalCount.Should().Be(0);
        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ItemsAreOrderedByDateAscending()
    {
        var startDate = new DateOnly(2023, 1, 2);
        var ratesByDate = new Dictionary<DateOnly, IReadOnlyDictionary<string, decimal>>
        {
            [startDate.AddDays(2)] = new Dictionary<string, decimal> { ["USD"] = 1.13m },
            [startDate]            = new Dictionary<string, decimal> { ["USD"] = 1.11m },
            [startDate.AddDays(1)] = new Dictionary<string, decimal> { ["USD"] = 1.12m },
        };
        var data = new HistoricalRatesRangeData(1m, "EUR", startDate, startDate.AddDays(2), ratesByDate);

        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var result = await _handler.Handle(
            new GetHistoricalRatesRangeQuery(startDate, startDate.AddDays(2)),
            CancellationToken.None);

        result.Data!.Items.Select(i => i.Date).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_TotalPagesRoundsUp()
    {
        var data = BuildRangeData(7);
        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var result = await _handler.Handle(
            new GetHistoricalRatesRangeQuery(data.StartDate, data.EndDate, "EUR", Page: 1, PageSize: 3),
            CancellationToken.None);

        result.Data!.TotalPages.Should().Be(3); // ceil(7/3) = 3
    }

    [Fact]
    public async Task Handle_MapsBaseAndDateRange()
    {
        var data = BuildRangeData(2);
        _service.GetHistoricalRatesRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<CancellationToken>())
            .Returns(data);

        var result = await _handler.Handle(
            new GetHistoricalRatesRangeQuery(data.StartDate, data.EndDate, "EUR"),
            CancellationToken.None);

        result.Data!.Base.Should().Be("EUR");
        result.Data.StartDate.Should().Be(data.StartDate);
        result.Data.EndDate.Should().Be(data.EndDate);
    }
}
