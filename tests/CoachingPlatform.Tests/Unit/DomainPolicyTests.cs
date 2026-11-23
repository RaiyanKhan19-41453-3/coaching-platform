using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Services;
using Xunit;

namespace CoachingPlatform.Tests.Unit;

public class DomainPolicyTests
{
    [Fact]
    public void FeeCalculator_AppliesDiscountThenTax()
    {
        var result = FeeCalculator.Calculate(
            [new Money(10000m, "BDT")], 1000m, 5m, "BDT");
        Assert.Equal(10000m, result.SubTotal.Amount);
        Assert.Equal(1000m, result.Discount.Amount);
        Assert.Equal(450m, result.Tax.Amount);
        Assert.Equal(9450m, result.Total.Amount);
    }

    [Fact]
    public void FeeCalculator_ClampsInvalidInputs()
    {
        var result = FeeCalculator.Calculate([new Money(100m, "BDT")], -50m, 150m, "BDT");
        Assert.Equal(0m, result.Discount.Amount);
        Assert.Equal(100m, result.Tax.Amount);
        Assert.Equal(200m, result.Total.Amount);
    }

    [Fact]
    public void AttendancePolicy_CountsUnmarkedAsAbsent()
        => Assert.Equal(3, AttendancePolicy.Absent(4, 1, 0, 0));
}
