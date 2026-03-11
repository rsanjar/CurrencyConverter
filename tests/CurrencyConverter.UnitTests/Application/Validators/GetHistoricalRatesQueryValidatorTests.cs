using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetHistoricalRates;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CurrencyConverter.UnitTests.Application.Validators;

public class GetHistoricalRatesQueryValidatorTests
{
    private readonly GetHistoricalRatesQueryValidator _validator = new();

    private static readonly DateOnly EarliestDate = new(1999, 1, 4);

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var query = new GetHistoricalRatesQuery(new DateOnly(2023, 6, 15), "USD");

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EarliestAllowedDate_Passes()
    {
        var result = _validator.TestValidate(new GetHistoricalRatesQuery(EarliestDate, "EUR"));

        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Validate_TodayDate_Passes()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = _validator.TestValidate(new GetHistoricalRatesQuery(today, "EUR"));

        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Validate_DateBeforeEarliest_FailsWithEarliestDateMessage()
    {
        var query = new GetHistoricalRatesQuery(new DateOnly(1999, 1, 3), "EUR");

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("Date must be on or after 1999-01-04 (Frankfurter earliest available date).");
    }

    [Fact]
    public void Validate_FutureDate_FailsWithFutureMessage()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var result = _validator.TestValidate(new GetHistoricalRatesQuery(futureDate, "EUR"));

        result.ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("Date cannot be in the future.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    public void Validate_EmptyBaseCurrency_Fails(string? baseCurrency)
    {
        var query = new GetHistoricalRatesQuery(new DateOnly(2023, 6, 15), baseCurrency!);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage("Base currency is required.");
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Validate_WrongLengthBaseCurrency_Fails(string baseCurrency)
    {
        var result = _validator.TestValidate(new GetHistoricalRatesQuery(new DateOnly(2023, 6, 15), baseCurrency));

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency);
    }

    [Theory]
    [InlineData("1US")]
    [InlineData("EU2")]
    public void Validate_NonLetterBaseCurrency_Fails(string baseCurrency)
    {
        var result = _validator.TestValidate(new GetHistoricalRatesQuery(new DateOnly(2023, 6, 15), baseCurrency));

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage("Base currency must contain only letters.");
    }
}
