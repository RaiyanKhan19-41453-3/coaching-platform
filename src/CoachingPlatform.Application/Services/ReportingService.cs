using CoachingPlatform.Application.DTOs.Reporting;
using CoachingPlatform.Application.Features.Reporting;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IReportingService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceReportItem>> GetAttendanceAsync(AttendanceReportQuery query, CancellationToken ct = default);
    Task<FeeReportResponse> GetFeesAsync(FeeReportQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<UtilizationItem>> GetUtilizationAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GradeReportItem>> GetGradesAsync(GradeReportQuery query, CancellationToken ct = default);
}

public sealed class ReportingService(ISender sender) : IReportingService
{
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetDashboardSummary(), ct);
        return new DashboardResponse(result.Students, result.Batches, result.Collected,
            result.Outstanding, result.AttendancePercent, result.LowAttendanceCount);
    }

    public async Task<IReadOnlyList<AttendanceReportItem>> GetAttendanceAsync(AttendanceReportQuery query, CancellationToken ct)
    {
        var result = await sender.Send(new GetAttendanceReport(query.BatchId, query.StudentId, query.From, query.To), ct);
        return result.Select(r => new AttendanceReportItem(r.StudentId, r.BatchId, r.Period,
            r.Sessions, r.Present, r.Absent, r.Late, r.Excused, r.AttendancePercent)).ToList();
    }

    public async Task<FeeReportResponse> GetFeesAsync(FeeReportQuery query, CancellationToken ct)
    {
        var result = await sender.Send(new GetFeeReport(query.StudentId, query.From, query.To), ct);
        var rows = result.Rows.Select(r => new FeeReportItem(r.StudentId, r.Period, r.Invoiced,
            r.Paid, r.Outstanding, r.Currency, r.InvoiceCount)).ToList();
        return new FeeReportResponse(rows, result.TotalInvoiced, result.TotalPaid,
            result.TotalOutstanding, result.Currency);
    }

    public async Task<IReadOnlyList<UtilizationItem>> GetUtilizationAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetUtilizationReport(), ct);
        return result.Select(r => new UtilizationItem(r.BatchId, r.Capacity, r.Enrolled,
            r.Waitlisted, r.UtilizationPercent)).ToList();
    }

    public async Task<IReadOnlyList<GradeReportItem>> GetGradesAsync(GradeReportQuery query, CancellationToken ct)
    {
        var result = await sender.Send(new GetGradeReport(query.StudentId, query.SubjectId), ct);
        return result.Select(r => new GradeReportItem(r.StudentId, r.SubjectId, r.Average,
            r.Highest, r.Lowest, r.Assessments, r.MarksObtained, r.MaxMarks)).ToList();
    }
}
