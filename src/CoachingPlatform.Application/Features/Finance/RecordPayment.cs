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

public sealed record RecordPayment(Guid InvoiceId, decimal Amount, string Currency, PaymentMethod Method, string? GatewayTransactionId)
    : IRequest<Guid>, IRequiresTenant;

public sealed class RecordPaymentValidator : AbstractValidator<RecordPayment>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).Length(3);
    }
}

public sealed class RecordPaymentHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<RecordPayment, Guid>
{
    public async Task<Guid> Handle(RecordPayment request, CancellationToken ct)
    {
        var invoice = await db.Set<Invoice>().FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
                      ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);
        var currency = request.Currency.ToUpperInvariant();
        if (invoice.Total.Currency != currency)
            throw new ConflictException($"Invoice currency is {invoice.Total.Currency}, got {currency}.");
        if (!string.IsNullOrWhiteSpace(request.GatewayTransactionId) &&
            await db.Set<Payment>().AnyAsync(p => p.GatewayTransactionId == request.GatewayTransactionId, ct))
            throw new ConflictException("Gateway transaction was already recorded.");
        if (invoice.Status is InvoiceStatus.Void or InvoiceStatus.Refunded)
            throw new ConflictException("Cannot pay a void or refunded invoice.");

        var amount = new Money(request.Amount, currency);
        var paid = invoice.AmountPaid.Add(amount);
        if (paid.Amount > invoice.Total.Amount)
            throw new ConflictException("Payment exceeds the invoice total.");

        var payment = new Payment
        {
            TenantId = current.TenantId,
            InvoiceId = invoice.Id,
            StudentId = invoice.StudentId,
            Amount = amount,
            Method = request.Method,
            Gateway = request.Method == PaymentMethod.OnlineGateway ? "manual-gateway" : null,
            GatewayTransactionId = request.GatewayTransactionId,
            Status = PaymentStatus.Completed,
            PaidAt = DateTimeOffset.UtcNow,
            ProcessedBy = current.UserId
        };
        db.Set<Payment>().Add(payment);

        invoice.AmountPaid = paid;
        invoice.Status = paid.Amount == invoice.Total.Amount
            ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        // Double entry: cash/bank debit, fee income credit.
        var postedAt = DateTimeOffset.UtcNow;
        db.Set<LedgerEntry>().Add(new LedgerEntry
        { TenantId = current.TenantId,
            AccountCode = "1000-CASH", Description = $"Payment {payment.Id}",
            Debit = new Money(amount.Amount, currency), Credit = Money.Zero(currency),
            SourceType = nameof(Payment), SourceId = payment.Id, PostedAt = postedAt
        });
        db.Set<LedgerEntry>().Add(new LedgerEntry
        { TenantId = current.TenantId,
            AccountCode = "4000-FEES", Description = $"Payment {payment.Id}",
            Debit = Money.Zero(currency), Credit = new Money(amount.Amount, currency),
            SourceType = nameof(Payment), SourceId = payment.Id, PostedAt = postedAt
        });
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId,
            EventType = EventTypes.PaymentReceived,
            PayloadJson = JsonSerializer.Serialize(new
            { PaymentId = payment.Id, InvoiceId = invoice.Id, invoice.StudentId, PaidAt = postedAt, Amount = amount.Amount, currency }),
            OccurredAt = postedAt
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Invoice changed concurrently; please retry.");
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Payment could not be recorded (possible duplicate transaction).");
        }
        return payment.Id;
    }
}
