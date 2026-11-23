using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Finance;

public sealed record InvoiceLineRequest(
    string Description, int Quantity, decimal UnitPrice, string Currency, Guid? FeeStructureId = null);

public sealed record CreateInvoiceRequest(
    Guid StudentId, Guid? EnrollmentId, IReadOnlyList<InvoiceLineRequest> Lines,
    decimal Discount, decimal TaxPercent);

public sealed record RecordPaymentRequest(
    decimal Amount, string Currency, PaymentMethod Method, string? GatewayTransactionId);
