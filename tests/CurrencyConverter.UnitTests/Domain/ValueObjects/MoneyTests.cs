using CurrencyConverter.Domain.ValueObjects;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    [Theory]
    [InlineData(100, "USD")]
    [InlineData(0, "EUR")]
    [InlineData(0.01, "GBP")]
    [InlineData(999999.99, "JPY")]
    public void Create_WithValidArgs_Succeeds(decimal amount, string currency)
    {
        var money = Money.Create(amount, currency);

        money.Amount.Should().Be(amount);
        money.Currency.Value.Should().Be(currency);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Create_WithNegativeAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var act = () => Money.Create(amount, "USD");

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Amount cannot be negative*");
    }

    [Fact]
    public void Create_WithZeroAmount_Succeeds()
    {
        var money = Money.Create(0, "EUR");

        money.Amount.Should().Be(0);
    }

    [Fact]
    public void Create_WithInvalidCurrencyCode_ThrowsArgumentException()
    {
        var act = () => Money.Create(100, "INVALID");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Convert_WithPositiveRate_ReturnsCorrectAmount()
    {
        var money = Money.Create(100, "EUR");

        var result = money.Convert(1.1m, "USD");

        result.Amount.Should().Be(110m);
        result.Currency.Value.Should().Be("USD");
    }

    [Fact]
    public void Convert_ChangesTargetCurrency()
    {
        var money = Money.Create(50, "EUR");

        var result = money.Convert(0.85m, "GBP");

        result.Currency.Value.Should().Be("GBP");
        result.Amount.Should().Be(42.5m);
    }

    [Fact]
    public void Convert_WithZeroAmount_ReturnsZero()
    {
        var money = Money.Create(0, "EUR");

        var result = money.Convert(1.5m, "USD");

        result.Amount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(-1)]
    public void Convert_WithNonPositiveRate_ThrowsArgumentOutOfRangeException(decimal rate)
    {
        var money = Money.Create(100, "EUR");

        var act = () => money.Convert(rate, "USD");

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Conversion rate must be positive*");
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_AreEqual()
    {
        var a = Money.Create(100, "EUR");
        var b = Money.Create(100, "EUR");

        a.Should().Be(b);
    }

    [Fact]
    public void Equals_DifferentAmount_AreNotEqual()
    {
        var a = Money.Create(100, "EUR");
        var b = Money.Create(200, "EUR");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_DifferentCurrency_AreNotEqual()
    {
        var a = Money.Create(100, "EUR");
        var b = Money.Create(100, "USD");

        a.Should().NotBe(b);
    }

    [Fact]
    public void EqualityOperator_SameMoneyValues_ReturnsTrue()
    {
        var a = Money.Create(100, "EUR");
        var b = Money.Create(100, "EUR");

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_DifferentMoneyValues_ReturnsTrue()
    {
        var a = Money.Create(100, "EUR");
        var b = Money.Create(100, "USD");

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsAmountAndCurrency()
    {
        var money = Money.Create(100, "USD");

        money.ToString().Should().Be("100 USD");
    }

    [Fact]
    public void GetHashCode_EqualMoney_ProduceSameHash()
    {
        var a = Money.Create(100, "EUR");
        var b = Money.Create(100, "EUR");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
