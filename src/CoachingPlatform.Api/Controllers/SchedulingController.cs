using CoachingPlatform.Application.DTOs.Scheduling;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public sealed class SchedulingController(ISchedulingService scheduling) : ControllerBase
{
    [HttpPost("schedule-rules")]
    public async Task<IActionResult> CreateRule([FromBody] CreateScheduleRuleRequest request, CancellationToken ct)
        => Ok(await scheduling.CreateScheduleRuleAsync(request, ct));

    [HttpPost("schedule-rules/{ruleId:guid}/sessions")]
    public async Task<IActionResult> Generate([FromRoute] Guid ruleId,
        [FromBody] GenerateSessionsRequest request, CancellationToken ct)
        => Ok(new { created = await scheduling.GenerateSessionsAsync(ruleId, request, ct) });

    [HttpPost("sessions/{sessionId:guid}/attendance")]
    public async Task<IActionResult> MarkAttendance([FromRoute] Guid sessionId,
        [FromBody] MarkAttendanceRequest request, CancellationToken ct)
        => Ok(new { written = await scheduling.MarkAttendanceAsync(sessionId, request, ct) });

    [HttpGet("sessions/{sessionId:guid}/attendance")]
    public async Task<IActionResult> GetAttendance([FromRoute] Guid sessionId, CancellationToken ct)
        => Ok(await scheduling.GetSessionAttendanceAsync(sessionId, ct));

    [HttpPost("leave-requests")]
    public async Task<IActionResult> CreateLeave([FromBody] CreateLeaveRequestRequest request, CancellationToken ct)
        => Ok(await scheduling.CreateLeaveRequestAsync(request, ct));

    [HttpPost("leave-requests/{id:guid}/review")]
    public async Task<IActionResult> ReviewLeave([FromRoute] Guid id,
        [FromBody] ReviewLeaveRequestRequest request, CancellationToken ct)
        => Ok(new { reviewed = await scheduling.ReviewLeaveRequestAsync(id, request, ct) });
}
