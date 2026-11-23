using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Features.Finance;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class FinanceTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Invoice_ComputesTotalsWithMoney()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var id = await Sender.Send(new CreateInvoice(student, null,
            new[]
            {
                new InvoiceLineInput("Tuition", 2, 5000m, "BDT"),
                new InvoiceLineInput("Lab", 1, 1000m, "BDT")
            },
            1000m, 5m));

        var invoice = await Db.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == id);
        Assert.Equal(11000m, invoice.SubTotal.Amount);
        Assert.Equal(1000m, invoice.Discount.Amount);
        Assert.Equal(500m, invoice.Tax.Amount);
        Assert.Equal(10500m, invoice.Total.Amount);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal("BDT", invoice.Total.Currency);
        Assert.Equal(2, invoice.Lines.Count);
    }

    [Fact]
    public async Task Payment_PartialThenFull_PostsLedgerAndUpdatesStatus()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var invoiceId = await Sender.Send(new CreateInvoice(student, null,
            new[] { new InvoiceLineInput("Fee", 1, 10000m, "BDT") }, 0m, 0m));

        await Sender.Send(new RecordPayment(invoiceId, 4000m, "BDT", PaymentMethod.Cash, null));
        var invoice = await Db.Invoices.SingleAsync(i => i.Id == invoiceId);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
        Assert.Equal(4000m, invoice.AmountPaid.Amount);

        await Sender.Send(new RecordPayment(invoiceId, 6000m, "BDT", PaymentMethod.Cash, null));
        invoice = await Db.Invoices.SingleAsync(i => i.Id == invoiceId);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(4, await Db.LedgerEntries.CountAsync());
        Assert.Equal(20000m, await Db.LedgerEntries.SumAsync(e => e.Credit.Amount + e.Debit.Amount));
    }

    [Fact]
    public async Task Payment_OverInvoiceTotal_ThrowsConflict()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var invoiceId = await Sender.Send(new CreateInvoice(student, null,
            new[] { new InvoiceLineInput("Fee", 1, 1000m, "BDT") }, 0m, 0m));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Sender.Send(new RecordPayment(invoiceId, 1001m, "BDT", PaymentMethod.Cash, null)));
        Assert.Empty(await Db.Payments.ToListAsync());
    }
}
