using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Crm;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Services;

/// <summary>Read-only list queries for the operations-console grids. Tenant isolation comes from the DbContext global filters.</summary>
public interface IListService
{
    Task<Guid> CreateAcademicYearAsync(string name, DateOnly start, DateOnly end, CancellationToken ct = default);
    Task<IReadOnlyList<object>> AcademicYearsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> CoursesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> ScheduleRulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> LeaveRequestsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> AssessmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> FeeStructuresAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> InvoicesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> DiscountsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> ScholarshipsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> FollowUpsAsync(CancellationToken ct = default);
}

public sealed class ListService(IApplicationDbContext db, ICurrentUser current) : IListService
{
    public async Task<Guid> CreateAcademicYearAsync(string name, DateOnly start, DateOnly end, CancellationToken ct)
    {
        var year = new AcademicYear
        {
            TenantId = current.TenantId,
            Name = name,
            StartDate = start,
            EndDate = end,
            IsActive = true,
        };
        db.Set<AcademicYear>().Add(year);
        await db.SaveChangesAsync(ct);
        return year.Id;
    }

    public async Task<IReadOnlyList<object>> AcademicYearsAsync(CancellationToken ct)
    {
        var rows = await db.Set<AcademicYear>().AsNoTracking().OrderByDescending(y => y.StartDate)
            .Select(y => new { y.Id, y.Name, y.StartDate, y.EndDate, y.IsActive })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> CoursesAsync(CancellationToken ct)
    {
        var rows = await db.Set<Course>().AsNoTracking().OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name, Level = c.Level.ToString(), c.DurationMonths, c.IsActive })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> ScheduleRulesAsync(CancellationToken ct)
    {
        var rows = await db.Set<ScheduleRule>().AsNoTracking().OrderBy(r => r.DayOfWeek).ThenBy(r => r.StartTime)
            .Select(r => new { r.Id, r.BatchId, r.DayOfWeek, StartTime = r.StartTime.ToString(), EndTime = r.EndTime.ToString(), r.IsActive })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> LeaveRequestsAsync(CancellationToken ct)
    {
        var rows = await db.Set<LeaveRequest>().AsNoTracking().OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.StudentId, Status = r.Status.ToString(), r.StartDate, r.EndDate, r.Reason })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> AssessmentsAsync(CancellationToken ct)
    {
        var rows = await db.Set<Assessment>().AsNoTracking().OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.Id, a.Title, Type = a.Type.ToString(), a.TotalMarks, Status = a.Status.ToString(), a.StartUtc, a.EndUtc })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> FeeStructuresAsync(CancellationToken ct)
    {
        var rows = await db.Set<FeeStructure>().AsNoTracking().OrderBy(f => f.Name)
            .Select(f => new { f.Id, f.Name, f.Code, Amount = f.Amount.Amount, Currency = f.Amount.Currency,
                Frequency = f.Frequency.ToString(), f.TaxPercent, f.EffectiveFrom, f.IsActive })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> InvoicesAsync(CancellationToken ct)
    {
        var rows = await db.Set<Invoice>().AsNoTracking().OrderByDescending(i => i.IssueDate)
            .Select(i => new { i.Id, i.InvoiceNumber, i.StudentId, Total = i.Total.Amount,
                AmountPaid = i.AmountPaid.Amount, Status = i.Status.ToString(), i.DueDate, i.IssueDate })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> DiscountsAsync(CancellationToken ct)
    {
        var rows = await db.Set<Discount>().AsNoTracking().OrderBy(d => d.Code)
            .Select(d => new { d.Id, d.Code, d.Name, Type = d.Type.ToString(), d.Value, d.UsageCount, d.IsActive })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> ScholarshipsAsync(CancellationToken ct)
    {
        var rows = await db.Set<Scholarship>().AsNoTracking().OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.Id, s.StudentId, s.Name, Type = s.Type.ToString(), s.Value, s.IsActive, s.StartDate })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }

    public async Task<IReadOnlyList<object>> FollowUpsAsync(CancellationToken ct)
    {
        var rows = await db.Set<FollowUp>().AsNoTracking().OrderByDescending(f => f.ScheduledAt)
            .Select(f => new { f.Id, f.LeadId, f.Note, f.ScheduledAt, Status = f.Status.ToString(), f.CompletedAt })
            .ToListAsync(ct);
        return rows.Cast<object>().ToList();
    }
}
