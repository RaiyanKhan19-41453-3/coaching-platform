using CoachingPlatform.Application.DTOs.Reporting;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
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
}
