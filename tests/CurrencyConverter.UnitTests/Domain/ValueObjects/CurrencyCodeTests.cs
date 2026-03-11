using CurrencyConverter.Domain.ValueObjects;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain.ValueObjects;

public class CurrencyCodeTests
{
    [Theory]
    [InlineData("EUR", "EUR")]
    [InlineData("USD", "USD")]
    [InlineData("GBP", "GBP")]
    [InlineData("eur", "EUR")]
    [InlineData("usd", "USD")]
    [InlineData(" GBP ", "GBP")]
    [InlineData("jpy", "JPY")]
    public void Create_WithValidInput_ReturnsNormalizedUppercaseCode(string input, string expected)
    {
        var result = CurrencyCode.Create(input);

        result.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        var act = () => CurrencyCode.Create(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNull_ThrowsArgumentException()
    {
        var act = () => CurrencyCode.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("EU1")]
    [InlineData("1US")]
    [InlineData("U$D")]
    public void Create_WithInvalidFormat_ThrowsArgumentException(string input)
    {
        var act = () => CurrencyCode.Create(input);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid ISO 4217*");
    }

    [Fact]
    public void Equals_SameCodes_AreEqual()
    {
        var a = CurrencyCode.Create("EUR");
        var b = CurrencyCode.Create("EUR");

        a.Should().Be(b);
    }

    [Fact]
    public void Equals_LowercaseAndUppercase_AreEqual()
    {
        var a = CurrencyCode.Create("eur");
        var b = CurrencyCode.Create("EUR");

        a.Should().Be(b);
    }

    [Fact]
    public void Equals_DifferentCodes_AreNotEqual()
    {
        var a = CurrencyCode.Create("EUR");
        var b = CurrencyCode.Create("USD");

        a.Should().NotBe(b);
    }

    [Fact]
    public void EqualityOperator_SameCodes_ReturnsTrue()
    {
        var a = CurrencyCode.Create("EUR");
        var b = CurrencyCode.Create("EUR");

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_DifferentCodes_ReturnsTrue()
    {
        var a = CurrencyCode.Create("EUR");
        var b = CurrencyCode.Create("USD");

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithNull_ReturnsFalse()
    {
        var a = CurrencyCode.Create("EUR");

        (a == null).Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var code = CurrencyCode.Create("EUR");

        code.ToString().Should().Be("EUR");
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsValue()
    {
        var code = CurrencyCode.Create("USD");
        string value = code;

        value.Should().Be("USD");
    }

    [Fact]
    public void GetHashCode_SameCodes_ProduceSameHash()
    {
        var a = CurrencyCode.Create("EUR");
        var b = CurrencyCode.Create("EUR");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentCodes_ProduceDifferentHashes()
    {
        var a = CurrencyCode.Create("EUR");
        var b = CurrencyCode.Create("USD");

        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }
}
