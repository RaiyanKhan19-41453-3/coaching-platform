using CoachingPlatform.Domain.Common;
using Xunit;

namespace CoachingPlatform.Tests.Unit;

public class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        var result = new Money(100.50m, "BDT").Add(new Money(25.25m, "BDT"));
        Assert.Equal(125.75m, result.Amount);
        Assert.Equal("BDT", result.Currency);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
        => Assert.Throws<InvalidOperationException>(() => new Money(1m, "BDT").Add(new Money(1m, "USD")));

    [Fact]
    public void Constructor_NormalizesCurrencyToUpperCase()
        => Assert.Equal("USD", new Money(10m, "usd").Currency);

    [Fact]
    public void Multiply_RoundsToTwoDecimals()
        => Assert.Equal(472.50m, new Money(9450m, "BDT").Multiply(0.05m).Amount);

    [Fact]
    public void Zero_HasZeroAmount()
        => Assert.True(Money.Zero("BDT").IsZero);
}
