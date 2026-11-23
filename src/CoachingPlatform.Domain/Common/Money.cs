namespace CoachingPlatform.Domain.Common;

/// <summary>
/// Value object stored as an owned EF type (Amount + Currency columns on the owning table).
/// Never store a bare decimal for money — currency and rounding rules travel with the value.
/// </summary>
public sealed class Money
{
    private Money() { }

    public Money(decimal amount, string currency = "BDT")
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BDT";

    public static Money Zero(string currency = "BDT") => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(decimal.Round(Amount * factor, 2), Currency);

    public bool IsZero => Amount == 0m;
    public bool IsNegative => Amount < 0m;

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Cannot mix {Currency} with {other.Currency}.");
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
