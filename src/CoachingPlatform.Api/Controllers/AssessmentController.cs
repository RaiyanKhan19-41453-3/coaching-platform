using CoachingPlatform.Application.DTOs.Assessment;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Admin,Teacher")]
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

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionRequest request, CancellationToken ct)
        => Ok(await assessment.CreateQuestionAsync(request, ct));

    [HttpPut("questions/{id:guid}")]
    public async Task<IActionResult> UpdateQuestion([FromRoute] Guid id,
        [FromBody] UpdateQuestionRequest request, CancellationToken ct)
        => Ok(new { updated = await assessment.UpdateQuestionAsync(id, request, ct) });

    [HttpPost("assessments/{assessmentId:guid}/questions")]
    public async Task<IActionResult> AddQuestion([FromRoute] Guid assessmentId,
        [FromBody] AddQuestionRequest request, CancellationToken ct)
        => Ok(await assessment.AddQuestionAsync(assessmentId, request, ct));

    [HttpGet("questions")]
    public async Task<IActionResult> ListQuestions([FromQuery] Guid? subjectId, [FromQuery] Guid? topicId, CancellationToken ct)
        => Ok(await assessment.ListQuestionsAsync(subjectId, topicId, ct));

    [HttpPost("attempts/{attemptId:guid}/answers/grade")]
    public async Task<IActionResult> GradeAnswer([FromRoute] Guid attemptId,
        [FromBody] GradeAnswerRequest request, CancellationToken ct)
        => Ok(new { graded = await assessment.GradeAnswerAsync(attemptId, request, ct) });
}
