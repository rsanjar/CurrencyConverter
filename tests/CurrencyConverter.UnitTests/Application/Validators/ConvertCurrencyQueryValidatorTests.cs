using CurrencyConverter.Application.Features.ExchangeRates.Queries.ConvertCurrency;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CurrencyConverter.UnitTests.Application.Validators;

public class ConvertCurrencyQueryValidatorTests
{
    private readonly ConvertCurrencyQueryValidator _validator = new();

    private static ConvertCurrencyQuery ValidQuery() =>
        new(100m, "EUR", ["USD", "GBP"]);

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.TestValidate(ValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_SingleToCurrency_Passes()
    {
        var result = _validator.TestValidate(ValidQuery() with { ToCurrencies = ["USD"] });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ManyToCurrencies_Passes()
    {
        var result = _validator.TestValidate(ValidQuery() with { ToCurrencies = ["USD", "GBP", "JPY", "CHF"] });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Validate_NonPositiveAmount_FailsWithMessage(decimal amount)
    {
        var result = _validator.TestValidate(ValidQuery() with { Amount = amount });

        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void Validate_PositiveAmount_Passes()
    {
        var result = _validator.TestValidate(ValidQuery() with { Amount = 0.01m });

        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    public void Validate_EmptyFromCurrency_FailsWithRequiredMessage(string? from)
    {
        var result = _validator.TestValidate(ValidQuery() with { FromCurrency = from! });

        result.ShouldHaveValidationErrorFor(x => x.FromCurrency)
            .WithErrorMessage("Source currency is required.");
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Validate_WrongLengthFromCurrency_Fails(string from)
    {
        var result = _validator.TestValidate(ValidQuery() with { FromCurrency = from });

        result.ShouldHaveValidationErrorFor(x => x.FromCurrency)
            .WithErrorMessage("Source currency must be a 3-letter ISO 4217 code.");
    }

    [Theory]
    [InlineData("1US")]
    [InlineData("E1R")]
    public void Validate_NumericFromCurrency_Fails(string from)
    {
        var result = _validator.TestValidate(ValidQuery() with { FromCurrency = from });

        result.ShouldHaveValidationErrorFor(x => x.FromCurrency)
            .WithErrorMessage("Source currency must contain only letters.");
    }

    [Fact]
    public void Validate_EmptyToCurrenciesList_FailsWithAtLeastOneMessage()
    {
        var result = _validator.TestValidate(ValidQuery() with { ToCurrencies = [] });

        result.ShouldHaveValidationErrorFor(x => x.ToCurrencies)
            .WithErrorMessage("At least one target currency must be provided.");
    }

    [Fact]
    public void Validate_InvalidToCurrencyElement_Fails()
    {
        var result = _validator.TestValidate(ValidQuery() with { ToCurrencies = ["USD", "INVALID"] });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NumericToCurrencyElement_Fails()
    {
        var result = _validator.TestValidate(ValidQuery() with { ToCurrencies = ["1US"] });

        result.IsValid.Should().BeFalse();
    }
}
