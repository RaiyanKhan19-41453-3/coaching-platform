using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Domain.Services;

/// <summary> Pure domain rules. No EF, no HTTP, no DI — safe to reuse and unit test. </summary>
public static class FeeCalculator
{
    public sealed record Breakdown(Money SubTotal, Money Discount, Money Tax, Money Total);

    public static Breakdown Calculate(
        IReadOnlyCollection<Money> lineAmounts, decimal discount, decimal taxPercent, string currency)
    {
        var subTotal = lineAmounts.Aggregate(Money.Zero(currency), (sum, amount) => sum.Add(amount));
        var safeDiscount = new Money(decimal.Round(Math.Clamp(discount, 0m, subTotal.Amount), 2), currency);
        var tax = subTotal.Subtract(safeDiscount).Multiply(Math.Clamp(taxPercent, 0m, 100m) / 100m);
        return new Breakdown(subTotal, safeDiscount, tax, subTotal.Subtract(safeDiscount).Add(tax));
    }
}

public static class AttendancePolicy
{
    public static decimal Percentage(int scheduled, int present, int late)
        => scheduled == 0 ? 0m : Math.Round((present + late) * 100m / scheduled, 2);

    public static int Absent(int scheduled, int present, int late, int excused)
        => Math.Max(0, scheduled - present - late - excused);
}

public static class EnrollmentPolicy
{
    public static bool HasCapacity(int activeCount, int capacity) => activeCount < capacity;
}
