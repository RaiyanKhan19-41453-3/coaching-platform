using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Finance;

public sealed record InvoiceLineRequest(
    string Description, int Quantity, decimal UnitPrice, string Currency, Guid? FeeStructureId = null);

public sealed record CreateInvoiceRequest(
    Guid StudentId, Guid? EnrollmentId, IReadOnlyList<InvoiceLineRequest> Lines,
    decimal Discount, decimal TaxPercent, string? DiscountCode = null);

public sealed record RecordPaymentRequest(
    decimal Amount, string Currency, PaymentMethod Method, string? GatewayTransactionId);

public sealed record RefundPaymentRequest(decimal Amount, string Reason);

public sealed record CreateFeeStructureRequest(Guid? CourseId, Guid? BatchId, string Name, string? Code,
    decimal Amount, string Currency, FeeFrequency Frequency, decimal TaxPercent,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo);

public sealed record GenerateInstallmentsRequest(Guid StudentId, int Count, DateOnly FirstDueDate);

public sealed record CreateDiscountRequest(string Code, string Name, DiscountType Type, decimal Value,
    decimal? MaxDiscountAmount, Guid? CourseId, DateOnly ValidFrom, DateOnly? ValidTo, int? UsageLimit);

public sealed record CreateScholarshipRequest(Guid StudentId, string Name, DiscountType Type,
    decimal Value, DateOnly StartDate, DateOnly? EndDate);
