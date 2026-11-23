using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Domain.Platform;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Finance;

public sealed record RefundPayment(Guid PaymentId, decimal Amount, string Reason)
    : IRequest<Guid>, IRequiresTenant;

public sealed class RefundPaymentValidator : AbstractValidator<RefundPayment>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RefundPaymentHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<RefundPayment, Guid>
{
    public async Task<Guid> Handle(RefundPayment request, CancellationToken ct)
    {
        var payment = await db.Set<Payment>().FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct)
                      ?? throw new NotFoundException(nameof(Payment), request.PaymentId);
        if (payment.Status != PaymentStatus.Completed)
            throw new ConflictException("Only completed payments can be refunded.");

        var already = await db.Set<Refund>().Where(r => r.PaymentId == payment.Id)
            .SumAsync(r => r.Amount.Amount, ct);
        var currency = payment.Amount.Currency;
        if (request.Amount + already > payment.Amount.Amount)
            throw new ConflictException("Refund exceeds the payment amount.");

        var refund = new Refund
        {
            TenantId = current.TenantId, PaymentId = payment.Id, StudentId = payment.StudentId,
            Amount = new Money(request.Amount, currency), Reason = request.Reason,
            Status = RefundStatus.Completed, ProcessedBy = current.UserId,
            ProcessedAt = DateTimeOffset.UtcNow
        };
        db.Set<Refund>().Add(refund);

        var posted = DateTimeOffset.UtcNow;
        db.Set<LedgerEntry>().Add(new LedgerEntry
        {
            TenantId = current.TenantId, AccountCode = "4000-FEES", Description = $"Refund {refund.Id}",
            Debit = new Money(refund.Amount.Amount, currency), Credit = Money.Zero(currency),
            SourceType = nameof(Refund), SourceId = refund.Id, PostedAt = posted
        });
        db.Set<LedgerEntry>().Add(new LedgerEntry
        {
            TenantId = current.TenantId, AccountCode = "1000-CASH", Description = $"Refund {refund.Id}",
            Debit = Money.Zero(currency), Credit = new Money(refund.Amount.Amount, currency),
            SourceType = nameof(Refund), SourceId = refund.Id, PostedAt = posted
        });

        if (payment.InvoiceId.HasValue)
        {
            var invoice = await db.Set<Invoice>().FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, ct);
            if (invoice is not null)
            {
                invoice.AmountPaid = invoice.AmountPaid.Subtract(refund.Amount);
                invoice.Status = invoice.AmountPaid.IsZero ? InvoiceStatus.Issued : InvoiceStatus.PartiallyPaid;
            }
        }
        payment.RefundedAmount = payment.RefundedAmount.Add(refund.Amount);
        if (payment.RefundedAmount.Amount >= payment.Amount.Amount) payment.Status = PaymentStatus.Refunded;
        else payment.Status = PaymentStatus.PartiallyRefunded;

        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId, EventType = EventTypes.PaymentRefunded,
            PayloadJson = JsonSerializer.Serialize(new
            { RefundId = refund.Id, PaymentId = payment.Id, payment.InvoiceId,
              payment.StudentId, Amount = request.Amount, Currency = currency }),
            OccurredAt = posted
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Payment changed concurrently; please retry.");
        }
        return refund.Id;
    }
}
