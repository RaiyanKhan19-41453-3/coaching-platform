using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Features.Academics;
using CoachingPlatform.Application.Features.Finance;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Tests.Fixtures;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

/// <summary>Proves the optimistic-concurrency guards hold when requests arrive together.</summary>
public class ConcurrencyTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task TwoConcurrentEnrollments_NeverOverbookBatch()
    {
        await NewTenantAsync();
        var s1 = await NewUserAsync();
        var s2 = await NewUserAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "B", "B-RACE",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), 1));

        using var scope1 = Services.CreateScope();
        using var scope2 = Services.CreateScope();
        var a = Attempt(scope1.ServiceProvider.GetRequiredService<ISender>(), new EnrollStudent(batchId, s1));
        var b = Attempt(scope2.ServiceProvider.GetRequiredService<ISender>(), new EnrollStudent(batchId, s2));
        await Task.WhenAll(a, b);

        Assert.True(await a ^ await b, "exactly one enrollment should succeed");
        Assert.Equal(1, await Db.Enrollments.CountAsync(e => e.BatchId == batchId));
    }

    [Fact]
    public async Task TwoConcurrentPayments_NeverExceedInvoiceTotal()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var invoiceId = await Sender.Send(new CreateInvoice(student, null,
            new[] { new InvoiceLineInput("Fee", 1, 1000m, "BDT") }, 0m, 0m));

        using var scope1 = Services.CreateScope();
        using var scope2 = Services.CreateScope();
        var a = Attempt(scope1.ServiceProvider.GetRequiredService<ISender>(),
            new RecordPayment(invoiceId, 600m, "BDT", PaymentMethod.Cash, null));
        var b = Attempt(scope2.ServiceProvider.GetRequiredService<ISender>(),
            new RecordPayment(invoiceId, 600m, "BDT", PaymentMethod.Cash, null));
        await Task.WhenAll(a, b);

        Assert.True(await a ^ await b, "exactly one payment should succeed");
        var invoice = await Db.Invoices.SingleAsync(i => i.Id == invoiceId);
        Assert.True(invoice.AmountPaid.Amount <= invoice.Total.Amount);
        Assert.Equal(1, await Db.Payments.CountAsync(p => p.InvoiceId == invoiceId));
        Assert.Equal(2, await Db.LedgerEntries.CountAsync());
    }

    private static async Task<bool> Attempt<T>(ISender sender, IRequest<T> request)
    {
        try { await sender.Send(request); return true; }
        catch (ConflictException) { return false; }
    }
}
