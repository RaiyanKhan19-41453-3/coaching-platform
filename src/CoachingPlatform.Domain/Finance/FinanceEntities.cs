using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;

namespace CoachingPlatform.Domain.Finance;

public sealed class FeeStructure : TenantEntity
{
    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public Money Amount { get; set; } = Money.Zero();
    public FeeFrequency Frequency { get; set; } = FeeFrequency.OneTime;
    public decimal TaxPercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Invoice : TenantEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid? EnrollmentId { get; set; }
    public string InvoiceNumber { get; set; } = default!;  // unique per tenant
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Money SubTotal { get; set; } = Money.Zero();
    public Money Discount { get; set; } = Money.Zero();
    public Money Tax { get; set; } = Money.Zero();
    public Money Total { get; set; } = Money.Zero();
    public Money AmountPaid { get; set; } = Money.Zero();
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Notes { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

public sealed class InvoiceLine : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = default!;
    public Guid? FeeStructureId { get; set; }
    public string Description { get; set; } = default!;
    public int Quantity { get; set; } = 1;
    public Money UnitPrice { get; set; } = Money.Zero();
    public Money Amount { get; set; } = Money.Zero();
    public decimal TaxPercent { get; set; }
    public int Order { get; set; }
}

public sealed class Payment : TenantEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public Money Amount { get; set; } = Money.Zero();
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Gateway { get; set; }
    public string? GatewayTransactionId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset? PaidAt { get; set; }
    public Money RefundedAmount { get; set; } = Money.Zero();
    public string? Reference { get; set; }
    public Guid? ProcessedBy { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class Discount : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DiscountType Type { get; set; } = DiscountType.Percentage;
    public decimal Value { get; set; }
    public Money MaxDiscountAmount { get; set; } = Money.Zero();
    public Guid? CourseId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Scholarship : TenantEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DiscountType Type { get; set; } = DiscountType.Percentage;
    public decimal Value { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ApprovedBy { get; set; }
}

/// <summary>Append-only double-entry ledger. Never update or delete a posted row.</summary>
public sealed class LedgerEntry : TenantEntity
{
    public string AccountCode { get; set; } = default!;   // e.g. "4000-FEES", "1000-CASH"
    public string? Description { get; set; }
    public Money Debit { get; set; } = Money.Zero();
    public Money Credit { get; set; } = Money.Zero();
    public string SourceType { get; set; } = default!;    // "Payment", "Expense", "Refund"
    public Guid? SourceId { get; set; }
    public DateTimeOffset PostedAt { get; set; }
    public bool IsReconciled { get; set; }
    public DateTimeOffset? ReconciledAt { get; set; }
    public Guid? ReconciledBy { get; set; }
}

public sealed class Expense : TenantEntity
{
    public string Category { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Money Amount { get; set; } = Money.Zero();
    public string? Vendor { get; set; }
    public DateOnly IncidentAt { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public Guid? RecordedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ReceiptStorageKey { get; set; }
}

public sealed class Refund : TenantEntity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = default!;
    public Guid StudentId { get; set; }
    public Money Amount { get; set; } = Money.Zero();
    public string Reason { get; set; } = default!;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public Guid? ProcessedBy { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? GatewayRefundId { get; set; }
}
