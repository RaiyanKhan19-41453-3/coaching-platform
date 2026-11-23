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

/// <summary>Creates N invoices from a fee structure, spaced by its frequency.</summary>
public sealed record GenerateInstallments(Guid FeeStructureId, Guid StudentId, int Count, DateOnly FirstDueDate)
    : IRequest<IReadOnlyList<Guid>>, IRequiresTenant;

public sealed class GenerateInstallmentsValidator : AbstractValidator<GenerateInstallments>
{
    public GenerateInstallmentsValidator()
    {
        RuleFor(x => x.FeeStructureId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 36);
    }
}

public sealed class GenerateInstallmentsHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<GenerateInstallments, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(GenerateInstallments request, CancellationToken ct)
    {
        var fee = await db.Set<FeeStructure>().FirstOrDefaultAsync(f => f.Id == request.FeeStructureId, ct)
                  ?? throw new NotFoundException(nameof(FeeStructure), request.FeeStructureId);
        var instalmentAmount = fee.Amount.Multiply(1m / request.Count).Amount;
        var currency = fee.Amount.Currency;
        Money Instalment() => new(instalmentAmount, currency);
        var ids = new List<Guid>();
        for (var i = 0; i < request.Count; i++)
        {
            var due = request.FirstDueDate.AddMonths(i * Months(fee.Frequency));
            var issue = due.AddDays(-14);
            var invoice = new Invoice
            {
                TenantId = current.TenantId, StudentId = request.StudentId,
                InvoiceNumber = $"INV-{due:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                IssueDate = issue, DueDate = due,
                SubTotal = Instalment(), Discount = Money.Zero(currency),
                Tax = Instalment().Multiply(fee.TaxPercent / 100m),
                Total = Instalment().Add(Instalment().Multiply(fee.TaxPercent / 100m)),
                AmountPaid = Money.Zero(currency), Status = InvoiceStatus.Issued
            };
            invoice.Lines.Add(new InvoiceLine
            {
                TenantId = current.TenantId, Description = $"{fee.Name} ({i + 1}/{request.Count})",
                Quantity = 1, UnitPrice = Instalment(), Amount = Instalment(),
                FeeStructureId = fee.Id, Order = i
            });
            db.Set<Invoice>().Add(invoice);
            ids.Add(invoice.Id);
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                TenantId = current.TenantId, EventType = EventTypes.InvoiceCreated,
                PayloadJson = JsonSerializer.Serialize(new
                { invoice.Id, invoice.InvoiceNumber, invoice.StudentId, invoice.IssueDate,
                  Total = invoice.Total.Amount, Currency = currency }),
                OccurredAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
        return ids;
    }

    private static int Months(FeeFrequency frequency) => frequency switch
    {
        FeeFrequency.Monthly => 1, FeeFrequency.Quarterly => 3,
        FeeFrequency.Termly => 4, FeeFrequency.Yearly => 12, _ => 1
    };
}
