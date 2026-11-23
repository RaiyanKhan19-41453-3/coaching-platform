using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Reporting;
using CoachingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs;

/// <summary>
/// Authoritative nightly rebuild of every projection from the write tables. Incremental
/// outbox updates keep reports fresh; this repairs drift and backfills older tenants.
/// </summary>
public sealed class ProjectionRebuilder(JobDbContextFactory factory, ILogger<ProjectionRebuilder> logger)
{
    public async Task RebuildAsync()
    {
        await using var scan = factory.Create(Guid.Empty);
        var tenantIds = await scan.Tenants.IgnoreQueryFilters().Select(t => t.Id).ToListAsync();

        foreach (var tenantId in tenantIds)
        {
            await using var db = factory.Create(tenantId);
            await db.AttendanceStats.ExecuteDeleteAsync();
            await db.FeeSummaries.ExecuteDeleteAsync();
            await db.BatchUtilizations.ExecuteDeleteAsync();
            await db.GradeSummaries.ExecuteDeleteAsync();

            var batches = await db.Batches.Select(b => new { b.Id, b.Capacity }).ToListAsync();
            var enrollments = await db.Enrollments
                .Select(e => new { e.BatchId, e.Status }).ToListAsync();
            foreach (var batch in batches)
            {
                var enrolled = enrollments.Count(e => e.BatchId == batch.Id &&
                    e.Status is EnrollmentStatus.Enrolled or EnrollmentStatus.Completed);
                db.BatchUtilizations.Add(new BatchUtilization
                {
                    TenantId = tenantId, BatchId = batch.Id, Capacity = batch.Capacity,
                    Enrolled = enrolled,
                    Waitlisted = enrollments.Count(e => e.BatchId == batch.Id && e.Status == EnrollmentStatus.Waitlisted),
                    UtilizationPercent = batch.Capacity == 0 ? 0m : Math.Round(enrolled * 100m / batch.Capacity, 2)
                });
            }

            var attendance = await (from a in db.Attendances
                                    join s in db.ClassSessions on a.SessionId equals s.Id
                                    select new
                                    {
                                        a.StudentId, s.BatchId, s.Id,
                                        Start = s.StartUtc, a.Status
                                    }).ToListAsync();
            foreach (var group in attendance.GroupBy(x => new
                     {
                         x.StudentId, x.BatchId,
                         Period = new DateOnly(x.Start.Year, x.Start.Month, 1)
                     }))
            {
                var rows = group.ToList();
                var sessions = rows.Select(r => r.Id).Distinct().Count();
                var present = rows.Count(r => r.Status == AttendanceStatus.Present);
                var late = rows.Count(r => r.Status == AttendanceStatus.Late);
                db.AttendanceStats.Add(new AttendanceStat
                {
                    TenantId = tenantId, StudentId = group.Key.StudentId, BatchId = group.Key.BatchId,
                    Period = group.Key.Period, Sessions = sessions, Present = present, Late = late,
                    Absent = rows.Count(r => r.Status == AttendanceStatus.Absent),
                    Excused = rows.Count(r => r.Status == AttendanceStatus.Excused),
                    AttendancePercent = sessions == 0 ? 0m : Math.Round((present + late) * 100m / sessions, 2)
                });
            }

            var invoices = await db.Invoices.Select(i => new
            {
                i.StudentId, i.IssueDate, Total = i.Total.Amount, Currency = i.Total.Currency
            }).ToListAsync();
            var payments = await db.Payments.Where(p => p.PaidAt != null).Select(p => new
            {
                p.StudentId, PaidAt = p.PaidAt!.Value, Amount = p.Amount.Amount
            }).ToListAsync();

            var feeKeys = invoices.Select(i => new { i.StudentId, Period = FirstOfMonth(i.IssueDate) })
                .Concat(payments.Select(p => new { p.StudentId, Period = FirstOfMonth(DateOnly.FromDateTime(p.PaidAt.UtcDateTime)) }))
                .Distinct();
            foreach (var key in feeKeys)
            {
                var inv = invoices.Where(i => i.StudentId == key.StudentId && FirstOfMonth(i.IssueDate) == key.Period).ToList();
                var paid = payments.Where(p => p.StudentId == key.StudentId &&
                    FirstOfMonth(DateOnly.FromDateTime(p.PaidAt.UtcDateTime)) == key.Period).Sum(p => p.Amount);
                var invoiced = inv.Sum(i => i.Total);
                var currency = inv.FirstOrDefault()?.Currency ?? "BDT";
                db.FeeSummaries.Add(new FeeSummary
                {
                    TenantId = tenantId, StudentId = key.StudentId, Period = key.Period,
                    Invoiced = new Money(invoiced, currency), Paid = new Money(paid, currency),
                    Outstanding = new Money(invoiced - paid, currency), InvoiceCount = inv.Count
                });
            }

            var grades = await db.GradeEntries.Select(g => new
            {
                g.StudentId, g.SubjectId, g.MarksObtained, g.MaxMarks
            }).ToListAsync();
            foreach (var group in grades.GroupBy(g => new { g.StudentId, g.SubjectId }))
            {
                var rows = group.ToList();
                var obtained = rows.Sum(r => r.MarksObtained);
                var max = rows.Sum(r => r.MaxMarks);
                db.GradeSummaries.Add(new GradeSummary
                {
                    TenantId = tenantId, StudentId = group.Key.StudentId, SubjectId = group.Key.SubjectId!.Value,
                    MarksObtained = obtained, MaxMarks = max, Assessments = rows.Count,
                    Average = max == 0 ? 0m : Math.Round(obtained * 100m / max, 2),
                    Highest = rows.Max(r => r.MarksObtained), Lowest = rows.Min(r => r.MarksObtained)
                });
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Rebuilt projections for tenant {TenantId}", tenantId);
        }
    }

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
}
