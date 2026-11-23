using CoachingPlatform.Application.DTOs.Finance;
using CoachingPlatform.Application.Features.Documents;
using CoachingPlatform.Application.Features.Finance;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IFinanceService
{
    Task<Guid> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task<Guid> RecordPaymentAsync(Guid invoiceId, RecordPaymentRequest request, CancellationToken ct = default);
    Task<Guid> RefundAsync(Guid paymentId, RefundPaymentRequest request, CancellationToken ct = default);
    Task<Guid> CreateFeeStructureAsync(CreateFeeStructureRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GenerateInstallmentsAsync(Guid feeStructureId, GenerateInstallmentsRequest request, CancellationToken ct = default);
    Task<Guid> CreateDiscountAsync(CreateDiscountRequest request, CancellationToken ct = default);
    Task<Guid> CreateScholarshipAsync(CreateScholarshipRequest request, CancellationToken ct = default);
    Task<byte[]> GetInvoicePdfAsync(Guid invoiceId, CancellationToken ct = default);
}

public sealed class FinanceService(ISender sender) : IFinanceService
{
    public Task<Guid> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct)
    {
        var lines = request.Lines.Select(l => new InvoiceLineInput(
            l.Description, l.Quantity, l.UnitPrice, l.Currency, l.FeeStructureId)).ToList();
        return sender.Send(new CreateInvoice(request.StudentId, request.EnrollmentId,
            lines, request.Discount, request.TaxPercent, request.DiscountCode), ct);
    }

    public Task<Guid> RecordPaymentAsync(Guid invoiceId, RecordPaymentRequest request, CancellationToken ct)
        => sender.Send(new RecordPayment(invoiceId, request.Amount, request.Currency,
            request.Method, request.GatewayTransactionId), ct);

    public Task<Guid> RefundAsync(Guid paymentId, RefundPaymentRequest request, CancellationToken ct)
        => sender.Send(new RefundPayment(paymentId, request.Amount, request.Reason), ct);

    public Task<Guid> CreateFeeStructureAsync(CreateFeeStructureRequest request, CancellationToken ct)
        => sender.Send(new CreateFeeStructure(request.CourseId, request.BatchId, request.Name, request.Code,
            request.Amount, request.Currency, request.Frequency, request.TaxPercent,
            request.EffectiveFrom, request.EffectiveTo), ct);

    public Task<IReadOnlyList<Guid>> GenerateInstallmentsAsync(Guid feeStructureId, GenerateInstallmentsRequest request, CancellationToken ct)
        => sender.Send(new GenerateInstallments(feeStructureId, request.StudentId, request.Count, request.FirstDueDate), ct);

    public Task<Guid> CreateDiscountAsync(CreateDiscountRequest request, CancellationToken ct)
        => sender.Send(new CreateDiscount(request.Code, request.Name, request.Type, request.Value,
            request.MaxDiscountAmount, request.CourseId, request.ValidFrom, request.ValidTo, request.UsageLimit), ct);

    public Task<Guid> CreateScholarshipAsync(CreateScholarshipRequest request, CancellationToken ct)
        => sender.Send(new CreateScholarship(request.StudentId, request.Name, request.Type, request.Value,
            request.StartDate, request.EndDate), ct);

    public Task<byte[]> GetInvoicePdfAsync(Guid invoiceId, CancellationToken ct)
        => sender.Send(new GenerateInvoicePdf(invoiceId), ct);
}
