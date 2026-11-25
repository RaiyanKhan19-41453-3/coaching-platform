using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize(Roles = "Admin,Teacher")]
[Produces("application/json")]
public sealed class JobsController(IJobService jobs) : ControllerBase
{
    [HttpPost("outbox/dispatch")]
    public async Task<IActionResult> Dispatch(CancellationToken ct)
    {
        await jobs.DispatchOutboxAsync(ct);
        return Ok(new { dispatched = true });
    }

    [HttpPost("projections/rebuild")]
    public async Task<IActionResult> Rebuild(CancellationToken ct)
    {
        await jobs.RebuildProjectionsAsync(ct);
        return Ok(new { rebuilt = true });
    }
}
