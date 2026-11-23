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
}
