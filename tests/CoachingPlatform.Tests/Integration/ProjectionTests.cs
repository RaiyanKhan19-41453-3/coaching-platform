using CoachingPlatform.Application.Features.Academics;
using CoachingPlatform.Application.Features.Finance;
using CoachingPlatform.Application.Features.Reporting;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Scheduling;
using CoachingPlatform.Infrastructure.Projections;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class ProjectionTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task BatchUtilization_ReflectsEnrollments()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "B", "B1",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), 2));
        await Sender.Send(new EnrollStudent(batchId, student));

        await Db.RefreshBatchUtilizationAsync(Ambient.TenantId, batchId);
        await Db.SaveChangesAsync();

        var projection = await Db.BatchUtilizations.SingleAsync(x => x.BatchId == batchId);
        Assert.Equal(1, projection.Enrolled);
        Assert.Equal(2, projection.Capacity);
        Assert.Equal(50m, projection.UtilizationPercent);
    }

    [Fact]
    public async Task AttendanceStat_ComputesAttendancePercentage()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "B", "B-ATT",
            new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31), 30));
        var period = new DateOnly(2026, 11, 1);
        for (var i = 0; i < 4; i++)
        {
            var session = new ClassSession
            {
                BatchId = batchId,
                StartUtc = new DateTimeOffset(2026, 11, 2 + i, 10, 0, 0, TimeSpan.Zero),
                EndUtc = new DateTimeOffset(2026, 11, 2 + i, 12, 0, 0, TimeSpan.Zero),
                Status = SessionStatus.Completed
            };
            Db.ClassSessions.Add(session);
            await Db.SaveChangesAsync();
            Db.Attendances.Add(new Attendance
            {
                SessionId = session.Id, StudentId = student,
                Status = i < 3 ? AttendanceStatus.Present : AttendanceStatus.Absent
            });
            await Db.SaveChangesAsync();
        }

        await Db.RefreshAttendanceAsync(Ambient.TenantId, student, batchId, period);
        await Db.SaveChangesAsync();

        var stat = await Db.AttendanceStats.SingleAsync(x => x.StudentId == student && x.BatchId == batchId);
        Assert.Equal(4, stat.Sessions);
        Assert.Equal(3, stat.Present);
        Assert.Equal(1, stat.Absent);
        Assert.Equal(75m, stat.AttendancePercent);
    }

    [Fact]
    public async Task FeeSummary_AggregatesInvoicesAndPayments()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var invoiceId = await Sender.Send(new CreateInvoice(student, null,
            new[] { new InvoiceLineInput("Fee", 1, 10000m, "BDT") }, 0m, 0m));
        await Sender.Send(new RecordPayment(invoiceId, 4000m, "BDT", PaymentMethod.Cash, null));

        var period = DateOnly.FromDateTime(DateTime.UtcNow);
        await Db.RefreshFeesAsync(Ambient.TenantId, student, period);
        await Db.SaveChangesAsync();

        var summary = await Db.FeeSummaries.SingleAsync(x => x.StudentId == student);
        Assert.Equal(10000m, summary.Invoiced.Amount);
        Assert.Equal(4000m, summary.Paid.Amount);
        Assert.Equal(6000m, summary.Outstanding.Amount);

        var dashboard = await Sender.Send(new GetDashboardSummary());
        Assert.Equal(4000m, dashboard.Collected);
        Assert.Equal(0, dashboard.Batches);
    }

    [Fact]
    public async Task AttendanceStat_CountsUnmarkedSessionsAsAbsent()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "B", "B-UNMARKED",
            new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31), 30));
        for (var i = 0; i < 4; i++)
        {
            var session = new ClassSession
            {
                BatchId = batchId,
                StartUtc = new DateTimeOffset(2026, 11, 3 + i, 10, 0, 0, TimeSpan.Zero),
                EndUtc = new DateTimeOffset(2026, 11, 3 + i, 12, 0, 0, TimeSpan.Zero),
                Status = SessionStatus.Completed
            };
            Db.ClassSessions.Add(session);
            await Db.SaveChangesAsync();
            if (i == 0)
            {
                Db.Attendances.Add(new Attendance
                {
                    SessionId = session.Id, StudentId = student, Status = AttendanceStatus.Present
                });
                await Db.SaveChangesAsync();
            }
        }

        await Db.RefreshAttendanceAsync(Ambient.TenantId, student, batchId, new DateOnly(2026, 11, 15));
        await Db.SaveChangesAsync();

        var stat = await Db.AttendanceStats.SingleAsync(x => x.StudentId == student);
        Assert.Equal(4, stat.Sessions);
        Assert.Equal(1, stat.Present);
        Assert.Equal(3, stat.Absent);
        Assert.Equal(25m, stat.AttendancePercent);
    }

    [Fact]
    public async Task AttendanceStat_ExcludesCancelledSessions()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "B", "B-CANCEL",
            new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31), 30));
        Db.ClassSessions.Add(new ClassSession
        {
            BatchId = batchId,
            StartUtc = new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero),
            EndUtc = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero),
            IsCancelled = true, Status = SessionStatus.Cancelled
        });
        await Db.SaveChangesAsync();

        await Db.RefreshAttendanceAsync(Ambient.TenantId, student, batchId, new DateOnly(2026, 11, 5));
        await Db.SaveChangesAsync();

        var stat = await Db.AttendanceStats.SingleAsync(x => x.StudentId == student);
        Assert.Equal(0, stat.Sessions);
        Assert.Equal(0m, stat.AttendancePercent);
    }
}
