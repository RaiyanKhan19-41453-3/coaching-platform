using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/parent")]
[Authorize]
[Produces("application/json")]
public sealed class ParentController(IParentService parent) : ControllerBase
{
    [HttpGet("children")]
    public async Task<IActionResult> Children(CancellationToken ct)
        => Ok(await parent.ListChildrenAsync(ct));

    [HttpGet("children/{studentId:guid}/summary")]
    public async Task<IActionResult> Summary([FromRoute] Guid studentId, CancellationToken ct)
        => Ok(await parent.GetChildSummaryAsync(studentId, ct));
}
