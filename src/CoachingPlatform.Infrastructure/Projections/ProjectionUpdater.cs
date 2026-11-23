using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Domain.Reporting;
using CoachingPlatform.Domain.Scheduling;
using CoachingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Infrastructure.Projections;

/// <summary>
/// Incremental projection maintenance. Called by outbox handlers so read models stay close
/// to real time; a nightly rebuild job repairs any drift.
/// </summary>
public static class ProjectionUpdater
{
    public static async Task RefreshAttendanceAsync(
        this AppDbContext db, Guid tenantId, Guid studentId, Guid batchId, DateOnly period, CancellationToken ct = default)
    {
        period = FirstOfMonth(period);
        var start = ToUtc(period);
        var end = ToUtc(period.AddMonths(1));
        var rows = await (from a in db.Attendances
                          join s in db.ClassSessions on a.SessionId equals s.Id
                          where a.StudentId == studentId && s.BatchId == batchId
                                && s.StartUtc >= start && s.StartUtc < end
                          select new { a.Status, s.Id }).ToListAsync(ct);

        var sessions = rows.Select(r => r.Id).Distinct().Count();
        var present = rows.Count(r => r.Status == AttendanceStatus.Present);
        var late = rows.Count(r => r.Status == AttendanceStatus.Late);
        var stat = await db.AttendanceStats.FirstOrDefaultAsync(
            x => x.StudentId == studentId && x.BatchId == batchId && x.Period == period, ct);
        if (stat is null)
        {
            stat = new AttendanceStat
            {
                TenantId = tenantId, StudentId = studentId, BatchId = batchId, Period = period
            };
            db.AttendanceStats.Add(stat);
        }
        stat.Sessions = sessions;
        stat.Present = present;
        stat.Absent = rows.Count(r => r.Status == AttendanceStatus.Absent);
        stat.Late = late;
        stat.Excused = rows.Count(r => r.Status == AttendanceStatus.Excused);
        stat.AttendancePercent = sessions == 0
            ? 0m : Math.Round((present + late) * 100m / sessions, 2);
    }

    public static async Task RefreshFeesAsync(
        this AppDbContext db, Guid tenantId, Guid studentId, DateOnly period, CancellationToken ct = default)
    {
        period = FirstOfMonth(period);
        var start = ToUtc(period);
        var end = ToUtc(period.AddMonths(1));
        var invoices = await db.Invoices
            .Where(i => i.StudentId == studentId && i.IssueDate >= period && i.IssueDate < period.AddMonths(1))
            .Select(i => new { i.Id, Total = i.Total.Amount, Currency = i.Total.Currency })
            .ToListAsync(ct);
        var payments = await db.Payments
            .Where(p => p.StudentId == studentId && p.PaidAt >= start && p.PaidAt < end)
            .Select(p => p.Amount.Amount).ToListAsync(ct);

        var currency = invoices.FirstOrDefault()?.Currency ?? "BDT";
        var invoiced = invoices.Sum(i => i.Total);
        var paid = payments.Sum();
        var summary = await db.FeeSummaries.FirstOrDefaultAsync(
            x => x.StudentId == studentId && x.Period == period, ct);
        if (summary is null)
        {
            summary = new FeeSummary { TenantId = tenantId, StudentId = studentId, Period = period };
            db.FeeSummaries.Add(summary);
        }
        summary.Invoiced = new Money(invoiced, currency);
        summary.Paid = new Money(paid, currency);
        summary.Outstanding = new Money(invoiced - paid, currency);
        summary.InvoiceCount = invoices.Count;
    }

    public static async Task RefreshBatchUtilizationAsync(
        this AppDbContext db, Guid tenantId, Guid batchId, CancellationToken ct = default)
    {
        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null) return;
        var statuses = await db.Enrollments.Where(e => e.BatchId == batchId)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);

        var enrolled = statuses.Where(s => s.Status is EnrollmentStatus.Enrolled or EnrollmentStatus.Completed)
            .Sum(s => s.Count);
        var waitlisted = statuses.Where(s => s.Status == EnrollmentStatus.Waitlisted).Sum(s => s.Count);
        var utilization = await db.BatchUtilizations.FirstOrDefaultAsync(x => x.BatchId == batchId, ct);
        if (utilization is null)
        {
            utilization = new BatchUtilization { TenantId = tenantId, BatchId = batchId };
            db.BatchUtilizations.Add(utilization);
        }
        utilization.Capacity = batch.Capacity;
        utilization.Enrolled = enrolled;
        utilization.Waitlisted = waitlisted;
        utilization.UtilizationPercent = batch.Capacity == 0
            ? 0m : Math.Round(enrolled * 100m / batch.Capacity, 2);
    }

    public static async Task RefreshGradesAsync(
        this AppDbContext db, Guid tenantId, Guid studentId, Guid subjectId, CancellationToken ct = default)
    {
        var marks = await db.GradeEntries
            .Where(g => g.StudentId == studentId && g.SubjectId == subjectId)
            .Select(g => new { g.MarksObtained, g.MaxMarks }).ToListAsync(ct);
        var summary = await db.GradeSummaries.FirstOrDefaultAsync(
            x => x.StudentId == studentId && x.SubjectId == subjectId, ct);
        if (summary is null)
        {
            summary = new GradeSummary { TenantId = tenantId, StudentId = studentId, SubjectId = subjectId };
            db.GradeSummaries.Add(summary);
        }
        var obtained = marks.Sum(m => m.MarksObtained);
        var max = marks.Sum(m => m.MaxMarks);
        summary.MarksObtained = obtained;
        summary.MaxMarks = max;
        summary.Assessments = marks.Count;
        summary.Average = max == 0 ? 0m : Math.Round(obtained * 100m / max, 2);
        summary.Highest = marks.Count == 0 ? 0m : marks.Max(m => m.MarksObtained);
        summary.Lowest = marks.Count == 0 ? 0m : marks.Min(m => m.MarksObtained);
    }

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static DateTimeOffset ToUtc(DateOnly date)
        => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
