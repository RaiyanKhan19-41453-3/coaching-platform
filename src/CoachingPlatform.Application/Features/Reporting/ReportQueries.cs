using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Reporting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Reporting;

// Reads never touch the write tables: every query targets a projection maintained by the outbox.
public sealed record GetDashboardSummary() : IRequest<DashboardDto>, IRequiresTenant;

public sealed record DashboardDto(
    int Students, int Batches, decimal Collected, decimal Outstanding,
    decimal AttendancePercent, int LowAttendanceCount);

public sealed class GetDashboardSummaryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDashboardSummary, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardSummary request, CancellationToken ct)
    {
        var students = await db.Set<Domain.Academics.Enrollment>().Select(e => e.StudentId).Distinct().CountAsync(ct);
        var batches = await db.Set<BatchUtilization>().CountAsync(ct);
        var fees = await db.Set<FeeSummary>().ToListAsync(ct);
        var attendance = await db.Set<AttendanceStat>().ToListAsync(ct);

        return new DashboardDto(
            students,
            batches,
            Math.Round(fees.Sum(f => f.Paid.Amount), 2),
            Math.Round(fees.Sum(f => f.Outstanding.Amount), 2),
            attendance.Count == 0 ? 0m : Math.Round(attendance.Average(a => a.AttendancePercent), 2),
            attendance.Count(a => a.AttendancePercent < 75m));
    }
}

public sealed record GetAttendanceReport(Guid? BatchId, Guid? StudentId, DateOnly? From, DateOnly? To)
    : IRequest<IReadOnlyList<AttendanceStatDto>>, IRequiresTenant;

public sealed record AttendanceStatDto(
    Guid StudentId, Guid BatchId, DateOnly Period, int Sessions,
    int Present, int Absent, int Late, int Excused, decimal AttendancePercent);

public sealed class GetAttendanceReportHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttendanceReport, IReadOnlyList<AttendanceStatDto>>
{
    public async Task<IReadOnlyList<AttendanceStatDto>> Handle(GetAttendanceReport request, CancellationToken ct)
    {
        var query = db.Set<AttendanceStat>().AsNoTracking().AsQueryable();
        if (request.BatchId.HasValue) query = query.Where(x => x.BatchId == request.BatchId.Value);
        if (request.StudentId.HasValue) query = query.Where(x => x.StudentId == request.StudentId.Value);
        if (request.From.HasValue) query = query.Where(x => x.Period >= request.From.Value);
        if (request.To.HasValue) query = query.Where(x => x.Period <= request.To.Value);
        return await query.OrderByDescending(x => x.Period)
            .Select(x => new AttendanceStatDto(x.StudentId, x.BatchId, x.Period, x.Sessions,
                x.Present, x.Absent, x.Late, x.Excused, x.AttendancePercent))
            .ToListAsync(ct);
    }
}

public sealed record GetFeeReport(Guid? StudentId, DateOnly? From, DateOnly? To)
    : IRequest<FeeReportDto>, IRequiresTenant;

public sealed record FeeRowDto(Guid StudentId, DateOnly Period, decimal Invoiced, decimal Paid,
    decimal Outstanding, string Currency, int InvoiceCount);

public sealed record FeeReportDto(
    IReadOnlyList<FeeRowDto> Rows, decimal TotalInvoiced, decimal TotalPaid, decimal TotalOutstanding, string Currency);

public sealed class GetFeeReportHandler(IApplicationDbContext db) : IRequestHandler<GetFeeReport, FeeReportDto>
{
    public async Task<FeeReportDto> Handle(GetFeeReport request, CancellationToken ct)
    {
        var query = db.Set<FeeSummary>().AsNoTracking().AsQueryable();
        if (request.StudentId.HasValue) query = query.Where(x => x.StudentId == request.StudentId.Value);
        if (request.From.HasValue) query = query.Where(x => x.Period >= request.From.Value);
        if (request.To.HasValue) query = query.Where(x => x.Period <= request.To.Value);
        var rows = await query.OrderByDescending(x => x.Period)
            .Select(x => new FeeRowDto(x.StudentId, x.Period, x.Invoiced.Amount, x.Paid.Amount,
                x.Outstanding.Amount, x.Invoiced.Currency, x.InvoiceCount))
            .ToListAsync(ct);
        return new FeeReportDto(rows,
            Math.Round(rows.Sum(r => r.Invoiced), 2),
            Math.Round(rows.Sum(r => r.Paid), 2),
            Math.Round(rows.Sum(r => r.Outstanding), 2),
            rows.FirstOrDefault()?.Currency ?? "BDT");
    }
}

public sealed record GetUtilizationReport() : IRequest<IReadOnlyList<UtilizationDto>>, IRequiresTenant;

public sealed record UtilizationDto(Guid BatchId, int Capacity, int Enrolled, int Waitlisted, decimal UtilizationPercent);

public sealed class GetUtilizationReportHandler(IApplicationDbContext db)
    : IRequestHandler<GetUtilizationReport, IReadOnlyList<UtilizationDto>>
{
    public async Task<IReadOnlyList<UtilizationDto>> Handle(GetUtilizationReport request, CancellationToken ct)
        => await db.Set<BatchUtilization>().AsNoTracking()
            .OrderByDescending(x => x.UtilizationPercent)
            .Select(x => new UtilizationDto(x.BatchId, x.Capacity, x.Enrolled, x.Waitlisted, x.UtilizationPercent))
            .ToListAsync(ct);
}

public sealed record GetGradeReport(Guid? StudentId, Guid? SubjectId)
    : IRequest<IReadOnlyList<GradeSummaryDto>>, IRequiresTenant;

public sealed record GradeSummaryDto(Guid StudentId, Guid SubjectId, decimal Average,
    decimal Highest, decimal Lowest, int Assessments, decimal MarksObtained, decimal MaxMarks);

public sealed class GetGradeReportHandler(IApplicationDbContext db)
    : IRequestHandler<GetGradeReport, IReadOnlyList<GradeSummaryDto>>
{
    public async Task<IReadOnlyList<GradeSummaryDto>> Handle(GetGradeReport request, CancellationToken ct)
    {
        var query = db.Set<GradeSummary>().AsNoTracking().AsQueryable();
        if (request.StudentId.HasValue) query = query.Where(x => x.StudentId == request.StudentId.Value);
        if (request.SubjectId.HasValue) query = query.Where(x => x.SubjectId == request.SubjectId.Value);
        return await query.OrderByDescending(x => x.Average)
            .Select(x => new GradeSummaryDto(x.StudentId, x.SubjectId, x.Average, x.Highest,
                x.Lowest, x.Assessments, x.MarksObtained, x.MaxMarks))
            .ToListAsync(ct);
    }
}
