using CoachingPlatform.Application.Features.Integration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

/// <summary>Receives events from sibling microservices (e.g. the proctored exam service).</summary>
[ApiController]
[Route("api/integrations")]
[AllowAnonymous]
public sealed class IntegrationController(ISender sender, IConfiguration configuration) : ControllerBase
{
    [HttpPost("exam-results")]
    public async Task<IActionResult> ExamResults([FromHeader(Name = "X-Integration-Key")] string? key,
        [FromBody] ExamResultsEvent body, CancellationToken ct)
    {
        var expected = configuration["Integration:ExamKey"];
        if (string.IsNullOrWhiteSpace(expected) || key != expected) return Unauthorized();

        foreach (var candidateId in body.CandidateIds.Distinct())
            await sender.Send(new RecordExternalNotification(body.TenantId, candidateId,
                "exam.results_published", "Exam results published",
                $"Results for exam {body.ExamId} are now available.", body.EventId), ct);
        return Ok(new { notified = body.CandidateIds.Distinct().Count() });
    }
}

public sealed record ExamResultsEvent(Guid TenantId, Guid ExamId, List<Guid> CandidateIds,
    int PublishedCount, Guid? EventId = null);
