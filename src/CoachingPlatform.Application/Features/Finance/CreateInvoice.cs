using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Tenancy;
using CoachingPlatform.Domain.Services;
using CoachingPlatform.Domain.Platform;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Finance;

public sealed record InvoiceLineInput(string Description, int Quantity, decimal UnitPrice, string Currency, Guid? FeeStructureId = null);
public sealed record CreateInvoice(Guid StudentId, Guid? EnrollmentId, IReadOnlyList<InvoiceLineInput> Lines, decimal Discount, decimal TaxPercent)
    : IRequest<Guid>, IRequiresTenant;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoice>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).InclusiveBetween(0m, 100m);
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
            l.RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
            l.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.Currency).Length(3);
        });
    }
}

public sealed class CreateInvoiceHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateInvoice, Guid>
{
    public async Task<Guid> Handle(CreateInvoice request, CancellationToken ct)
    {
        if (!await db.Set<User>().AnyAsync(u => u.Id == request.StudentId, ct))
            throw new NotFoundException(nameof(User), request.StudentId);

        var tenant = await db.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == current.TenantId, ct)
                     ?? throw new NotFoundException(nameof(Tenant), current.TenantId);

        var currency = request.Lines[0].Currency.ToUpperInvariant();
        if (currency != tenant.DefaultCurrency.ToUpperInvariant())
            throw new ConflictException($"This center invoices in {tenant.DefaultCurrency}.");
        if (request.Lines.Any(l => l.Currency.ToUpperInvariant() != currency))
            throw new ConflictException("All invoice lines must use the same currency.");

        var lines = request.Lines.Select((l, i) => new InvoiceLine
        {
            TenantId = current.TenantId,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = new Money(l.UnitPrice, currency),
            Amount = new Money(decimal.Round(l.UnitPrice * l.Quantity, 2), currency),
            FeeStructureId = l.FeeStructureId,
            TaxPercent = request.TaxPercent,
            Order = i
        }).ToList();

        if (request.EnrollmentId.HasValue)
        {
            var enrollment = await db.Set<Domain.Academics.Enrollment>()
                .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId.Value, ct)
                ?? throw new NotFoundException(nameof(Domain.Academics.Enrollment), request.EnrollmentId.Value);
            if (enrollment.StudentId != request.StudentId)
                throw new ConflictException("Enrollment belongs to a different student.");
        }

        var totals = FeeCalculator.Calculate(
            lines.Select(l => l.Amount).ToList(), request.Discount, request.TaxPercent, currency);
        var subTotal = totals.SubTotal;
        var discount = totals.Discount;
        var tax = totals.Tax;
        var total = totals.Total;

        var invoice = new Invoice
        {
            TenantId = current.TenantId,
            StudentId = request.StudentId,
            EnrollmentId = request.EnrollmentId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            SubTotal = subTotal,
            Discount = discount,
            Tax = tax,
            Total = total,
            AmountPaid = Money.Zero(currency),
            Status = InvoiceStatus.Issued,
            Lines = lines
        };
        db.Set<Invoice>().Add(invoice);
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId,
            EventType = EventTypes.InvoiceCreated,
            PayloadJson = JsonSerializer.Serialize(new { invoice.Id, invoice.InvoiceNumber, invoice.StudentId, invoice.IssueDate, Total = total.Amount, currency }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return invoice.Id;
    }
}
