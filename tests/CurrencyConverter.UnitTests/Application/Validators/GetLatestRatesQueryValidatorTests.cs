using CurrencyConverter.Application.Features.ExchangeRates.Queries.GetLatestRates;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CurrencyConverter.UnitTests.Application.Validators;

public class GetLatestRatesQueryValidatorTests
{
    private readonly GetLatestRatesQueryValidator _validator = new();

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("jpy")]
    [InlineData("cHf")]
    public void Validate_ValidBaseCurrency_Passes(string baseCurrency)
    {
        var result = _validator.TestValidate(new GetLatestRatesQuery(baseCurrency));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DefaultQuery_Passes()
    {
        var result = _validator.TestValidate(new GetLatestRatesQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    public void Validate_EmptyOrNullBaseCurrency_FailsWithRequiredMessage(string? baseCurrency)
    {
        var result = _validator.TestValidate(new GetLatestRatesQuery(baseCurrency!));

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage("Base currency is required.");
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("A")]
    [InlineData("ABCDE")]
    public void Validate_WrongLengthBaseCurrency_FailsWithLengthMessage(string baseCurrency)
    {
        var result = _validator.TestValidate(new GetLatestRatesQuery(baseCurrency));

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage("Base currency must be a 3-letter ISO 4217 code.");
    }

    [Theory]
    [InlineData("1US")]
    [InlineData("EU2")]
    [InlineData("12D")]
    [InlineData("1$3")]
    public void Validate_NumericOrSpecialCharBaseCurrency_FailsWithLettersMessage(string baseCurrency)
    {
        var result = _validator.TestValidate(new GetLatestRatesQuery(baseCurrency));

        result.ShouldHaveValidationErrorFor(x => x.BaseCurrency)
            .WithErrorMessage("Base currency must contain only letters.");
    }

    [Fact]
    public void Validate_WithTargetCurrencies_Passes()
    {
        var query = new GetLatestRatesQuery("EUR", ["USD", "GBP"]);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
