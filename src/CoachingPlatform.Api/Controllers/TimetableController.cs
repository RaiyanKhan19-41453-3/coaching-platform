using CoachingPlatform.Application.DTOs.Scheduling;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Admin,Teacher")]
[Produces("application/json")]
public sealed class TimetableController(ITimetableService timetable) : ControllerBase
{
    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest r, CancellationToken ct)
        => Ok(new { id = await timetable.CreateRoomAsync(r, ct) });
    [HttpGet("rooms")]
    public async Task<IActionResult> Rooms(CancellationToken ct)
        => Ok(await timetable.ListRoomsAsync(ct));
    [HttpGet("timetable")]
    public async Task<IActionResult> Get([FromQuery] Guid? batchId, [FromQuery] Guid? teacherId,
        [FromQuery] Guid? studentId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
        => Ok(await timetable.GetTimetableAsync(new TimetableQuery(batchId, teacherId, studentId, from, to), ct));
    [HttpPost("rooms/{id:guid}/availability")]
    public async Task<IActionResult> Available([FromRoute] Guid id, [FromBody] RoomAvailabilityRequest r, CancellationToken ct)
        => Ok(new { available = await timetable.IsRoomAvailableAsync(id, r, ct) });
}
