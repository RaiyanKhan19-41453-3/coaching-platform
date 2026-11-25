using CoachingPlatform.Application.DTOs.Reporting;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CoachingPlatform.Application.DTOs.Admin;
namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Teacher")]
[Produces("application/json")]
public sealed class ReportsController(IReportingService reports) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
        => Ok(await reports.GetDashboardAsync(ct));

    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] Guid? batchId, [FromQuery] Guid? studentId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await reports.GetAttendanceAsync(new AttendanceReportQuery(batchId, studentId, from, to), ct));

    [HttpGet("fees")]
    public async Task<IActionResult> Fees([FromQuery] Guid? studentId, [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await reports.GetFeesAsync(new FeeReportQuery(studentId, from, to), ct));

    [HttpGet("utilization")]
    public async Task<IActionResult> Utilization(CancellationToken ct)
        => Ok(await reports.GetUtilizationAsync(ct));

    [HttpGet("grades")]
    public async Task<IActionResult> Grades([FromQuery] Guid? studentId, [FromQuery] Guid? subjectId, CancellationToken ct)
        => Ok(await reports.GetGradesAsync(new GradeReportQuery(studentId, subjectId), ct));

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] string? entityName, [FromQuery] string? entityId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await reports.GetAuditLogsAsync(entityName, entityId, page, pageSize, ct));

    [HttpGet("features/{key}")]
    public async Task<IActionResult> IsEnabled([FromRoute] string key, CancellationToken ct)
        => Ok(new { enabled = await reports.IsFeatureEnabledAsync(key, ct) });

    [HttpPost("features")]
    public async Task<IActionResult> SetFlag([FromBody] FeatureFlagRequest request, CancellationToken ct)
        => Ok(await reports.SetFeatureFlagAsync(request, ct));

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await reports.SearchAsync(term, page, pageSize, ct));

    [HttpGet("report-cards")]
    public async Task<IActionResult> ReportCard([FromQuery] Guid studentId, [FromQuery] Guid batchId, CancellationToken ct)
    {
        var bytes = await reports.GenerateReportCardAsync(studentId, batchId, ct);
        return File(bytes, "application/pdf", $"report-{studentId}.pdf");
    }
}
