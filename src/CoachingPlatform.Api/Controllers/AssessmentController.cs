using CoachingPlatform.Application.DTOs.Assessment;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public sealed class AssessmentController(IAssessmentService assessment) : ControllerBase
{
    [HttpPost("assessments")]
    public async Task<IActionResult> Create([FromBody] CreateAssessmentRequest request, CancellationToken ct)
        => Ok(await assessment.CreateAssessmentAsync(request, ct));

    [HttpPost("assessments/{assessmentId:guid}/attempts")]
    public async Task<IActionResult> Submit([FromRoute] Guid assessmentId,
        [FromBody] SubmitAttemptRequest request, CancellationToken ct)
        => Ok(await assessment.SubmitAttemptAsync(assessmentId, request, ct));
}
