using CoachingPlatform.Application.DTOs.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public sealed class AcademicsController(IAcademicsService academics) : ControllerBase
{
    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken ct)
        => Ok(await academics.CreateCourseAsync(request, ct));

    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest request, CancellationToken ct)
        => Ok(await academics.CreateBatchAsync(request, ct));

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches([FromQuery] BatchStatus? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await academics.GetBatchesAsync(new BatchQuery(status, search, page, pageSize), ct));

    [HttpPost("batches/{batchId:guid}/enrollments")]
    public async Task<IActionResult> Enroll([FromRoute] Guid batchId,
        [FromBody] EnrollStudentRequest request, CancellationToken ct)
        => Ok(await academics.EnrollStudentAsync(batchId, request, ct));

    [HttpPost("enrollments/{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id,
        [FromBody] ChangeEnrollmentStatusRequest request, CancellationToken ct)
        => Ok(new { changed = await academics.ChangeEnrollmentStatusAsync(id, request, ct) });

    [HttpPost("batches/{batchId:guid}/teachers")]
    public async Task<IActionResult> AssignTeacher([FromRoute] Guid batchId,
        [FromBody] AssignTeacherRequest request, CancellationToken ct)
        => Ok(await academics.AssignTeacherAsync(batchId, request, ct));

    [HttpDelete("batches/{batchId:guid}/teachers/{teacherId:guid}")]
    public async Task<IActionResult> RemoveTeacher([FromRoute] Guid batchId, [FromRoute] Guid teacherId, CancellationToken ct)
        => Ok(new { removed = await academics.RemoveTeacherAsync(batchId, teacherId, ct) });

    [HttpGet("students/{studentId:guid}/export")]
    public async Task<IActionResult> Export([FromRoute] Guid studentId, CancellationToken ct)
        => Ok(new { json = await academics.ExportStudentDataAsync(studentId, ct) });

    [HttpPost("students/{studentId:guid}/erase")]
    public async Task<IActionResult> Erase([FromRoute] Guid studentId, CancellationToken ct)
        => Ok(new { erased = await academics.EraseStudentDataAsync(studentId, ct) });
}
