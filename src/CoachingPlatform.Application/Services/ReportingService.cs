using CoachingPlatform.Application.DTOs.Reporting;
using CoachingPlatform.Application.Features.Reporting;
using CoachingPlatform.Application.Features.Admin;
using CoachingPlatform.Application.Features.Documents;
using MediatR;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.DTOs.Admin;

namespace CoachingPlatform.Application.Services;

public interface IReportingService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceReportItem>> GetAttendanceAsync(AttendanceReportQuery query, CancellationToken ct = default);
    Task<FeeReportResponse> GetFeesAsync(FeeReportQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<UtilizationItem>> GetUtilizationAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GradeReportItem>> GetGradesAsync(GradeReportQuery query, CancellationToken ct = default);
    Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(string? entityName, string? entityId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> IsFeatureEnabledAsync(string key, CancellationToken ct = default);
    Task<Guid> SetFeatureFlagAsync(FeatureFlagRequest request, CancellationToken ct = default);
    Task<SearchResponse> SearchAsync(string term, int page, int pageSize, CancellationToken ct = default);
    Task<byte[]> GenerateReportCardAsync(Guid studentId, Guid batchId, CancellationToken ct = default);
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

    public async Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(string? entityName, string? entityId,
        int page, int pageSize, CancellationToken ct)
    {
        var result = await sender.Send(new GetAuditLogs(entityName, entityId, page, pageSize), ct);
        var items = result.Items.Select(a => new AuditLogResponse(a.Id, a.EntityName, a.EntityId,
            a.Action, a.ActorEmail, a.ChangesJson, a.OccurredAt)).ToList();
        return new PagedResult<AuditLogResponse>(items, result.Total, result.Page, result.PageSize);
    }

    public Task<bool> IsFeatureEnabledAsync(string key, CancellationToken ct)
        => sender.Send(new IsFeatureEnabled(key), ct);

    public Task<Guid> SetFeatureFlagAsync(FeatureFlagRequest request, CancellationToken ct)
        => sender.Send(new SetFeatureFlag(request.Key, request.Description, request.IsEnabled, request.TargetingJson), ct);

    public async Task<SearchResponse> SearchAsync(string term, int page, int pageSize, CancellationToken ct)
    {
        var result = await sender.Send(new Search(term, page, pageSize), ct);
        return new SearchResponse(
            result.Students.Select(x => new SearchItem(x.Id, x.Name)).ToList(),
            result.Courses.Select(x => new SearchItem(x.Id, x.Name)).ToList(),
            result.Batches.Select(x => new SearchItem(x.Id, x.Name)).ToList(),
            result.Assets.Select(x => new SearchItem(x.Id, x.Name)).ToList());
    }

    public Task<byte[]> GenerateReportCardAsync(Guid studentId, Guid batchId, CancellationToken ct)
        => sender.Send(new GenerateReportCard(studentId, batchId), ct);
}
