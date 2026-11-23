using CoachingPlatform.Application.Features.Communication;
using CoachingPlatform.Application.Features.Finance;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class ExtendedFeatureTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Refund_ReversesLedgerAndReducesInvoicePaid()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var invoiceId = await Sender.Send(new CreateInvoice(student, null,
            new[] { new InvoiceLineInput("Fee", 1, 1000m, "BDT") }, 0m, 0m));
        var paymentId = await Sender.Send(new RecordPayment(invoiceId, 1000m, "BDT", PaymentMethod.Cash, null));

        await Sender.Send(new RefundPayment(paymentId, 400m, "Requested by student"));

        var invoice = await Db.Invoices.SingleAsync(i => i.Id == invoiceId);
        Assert.Equal(600m, invoice.AmountPaid.Amount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
        Assert.Equal(PaymentStatus.PartiallyRefunded, (await Db.Payments.SingleAsync()).Status);
        Assert.Equal(4, await Db.LedgerEntries.CountAsync());
        Assert.Equal(400m, await Db.Refunds.SumAsync(r => r.Amount.Amount));
        Assert.Contains(await Db.OutboxMessages.ToListAsync(), m => m.EventType == "payment.refunded");
    }

    [Fact]
    public async Task Installments_CreateOneInvoicePerPeriod()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var structureId = await Sender.Send(new CreateFeeStructure(null, null, "Tuition", "TUITION",
            12000m, "BDT", FeeFrequency.Monthly, 0m, new DateOnly(2026, 1, 1), null));

        var ids = await Sender.Send(new GenerateInstallments(structureId, student, 3, new DateOnly(2026, 2, 1)));

        Assert.Equal(3, ids.Count);
        Assert.Equal(3, await Db.Invoices.CountAsync(i => i.StudentId == student));
        Assert.All(await Db.Invoices.ToListAsync(), i => Assert.Equal(4000m, i.Total.Amount));
    }

    [Fact]
    public async Task Announcement_CanBeCreatedAndListed()
    {
        await NewTenantAsync();
        var id = await Sender.Send(new CreateAnnouncement("Exam schedule", "Finals next week",
            null, AnnouncementPriority.High, true, null));
        var list = await Sender.Send(new ListAnnouncements());
        Assert.Contains(list, a => a.Id == id && a.Title == "Exam schedule");
    }

    [Fact]
    public async Task DiscountAndScholarship_AreAppliedToInvoice()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        await Sender.Send(new CreateDiscount("SAVE10", "Ten percent", DiscountType.Percentage,
            10m, null, null, new DateOnly(2026, 1, 1), null, null));
        await Sender.Send(new CreateScholarship(student, "Merit", DiscountType.Percentage,
            5m, new DateOnly(2026, 1, 1), null));

        var invoiceId = await Sender.Send(new CreateInvoice(student, null,
            new[] { new InvoiceLineInput("Fee", 1, 1000m, "BDT") }, 0m, 0m, "SAVE10"));
        var invoice = await Db.Invoices.SingleAsync(i => i.Id == invoiceId);
        Assert.Equal(150m, invoice.Discount.Amount);   // 10% + 5%
        Assert.Equal(850m, invoice.Total.Amount);
        Assert.Equal(1, (await Db.Discounts.SingleAsync()).UsageCount);
    }
}
