using CoachingPlatform.Application.DTOs.Finance;
using CoachingPlatform.Application.Features.Finance;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IFinanceService
{
    Task<Guid> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task<Guid> RecordPaymentAsync(Guid invoiceId, RecordPaymentRequest request, CancellationToken ct = default);
}

public sealed class FinanceService(ISender sender) : IFinanceService
{
    public Task<Guid> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct)
    {
        var lines = request.Lines.Select(l => new InvoiceLineInput(
            l.Description, l.Quantity, l.UnitPrice, l.Currency, l.FeeStructureId)).ToList();
        return sender.Send(new CreateInvoice(request.StudentId, request.EnrollmentId,
            lines, request.Discount, request.TaxPercent), ct);
    }

    public Task<Guid> RecordPaymentAsync(Guid invoiceId, RecordPaymentRequest request, CancellationToken ct)
        => sender.Send(new RecordPayment(invoiceId, request.Amount, request.Currency,
            request.Method, request.GatewayTransactionId), ct);
}
