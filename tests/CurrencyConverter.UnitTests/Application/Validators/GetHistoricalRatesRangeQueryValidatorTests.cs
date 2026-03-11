using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRatesRange;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CurrencyConverter.UnitTests.Application.Validators;

public class GetHistoricalRatesRangeQueryValidatorTests
{
    private readonly GetHistoricalRatesRangeQueryValidator _validator = new();

    private static readonly DateOnly EarliestDate = new(1999, 1, 4);

    private static GetHistoricalRatesRangeQuery ValidQuery() => new(
        new DateOnly(2023, 1, 2),
        new DateOnly(2023, 1, 31),
        "EUR",
        null,
        1,
        20);

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.TestValidate(ValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_StartDateBeforeEarliest_Fails()
    {
        var query = ValidQuery() with { StartDate = new DateOnly(1998, 12, 31) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.StartDate)
            .WithErrorMessage("Start date must be on or after 1999-01-04 (Frankfurter earliest available date).");
    }

    [Fact]
    public void Validate_EndDateBeforeStartDate_Fails()
    {
        var query = ValidQuery() with
        {
            StartDate = new DateOnly(2023, 6, 1),
            EndDate = new DateOnly(2023, 5, 1)
        };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be greater than or equal to start date.");
    }

    [Fact]
    public void Validate_StartEqualsEnd_Passes()
    {
        var query = ValidQuery() with
        {
            StartDate = new DateOnly(2023, 6, 1),
            EndDate = new DateOnly(2023, 6, 1)
        };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Validate_FutureStartDate_Fails()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var query = ValidQuery() with { StartDate = future, EndDate = future.AddDays(1) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void Validate_FutureEndDate_Fails()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var future = today.AddDays(2);
        var query = ValidQuery() with { StartDate = today, EndDate = future };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageLessThan1_Fails(int page)
    {
        var query = ValidQuery() with { Page = page };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("Page must be greater than or equal to 1.");
    }

    [Fact]
    public void Validate_Page1_Passes()
    {
        var query = ValidQuery() with { Page = 1 };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public void Validate_PageSizeOutOfRange_Fails(int pageSize)
    {
        var query = ValidQuery() with { PageSize = pageSize };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("Page size must be between 1 and 100.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_ValidPageSize_Passes(int pageSize)
    {
        var query = ValidQuery() with { PageSize = pageSize };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("")]
    [InlineData("1US")]
    public void Validate_InvalidBaseCurrency_Fails(string baseCurrency)
    {
        var query = ValidQuery() with { BaseCurrency = baseCurrency };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Fact]
    public void Validate_TargetCurrenciesWithEmptyElement_Fails()
    {
        var query = ValidQuery() with { TargetCurrencies = ["USD", ""] };

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NullTargetCurrencies_Passes()
    {
        var query = ValidQuery() with { TargetCurrencies = null };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
