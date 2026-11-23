using CoachingPlatform.Application.DTOs.Content;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
[Produces("application/json")]
public sealed class AssetsController(IAssetService assets) : ControllerBase
{
    [HttpPost("upload-url")]
    public async Task<IActionResult> RequestUpload([FromBody] RequestUploadRequest request, CancellationToken ct)
        => Ok(await assets.RequestUploadAsync(request, ct));

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken ct)
        => Ok(await assets.ConfirmUploadAsync(id, ct));

    [HttpGet("{id:guid}/url")]
    public async Task<IActionResult> DownloadUrl([FromRoute] Guid id, CancellationToken ct)
        => Ok(await assets.GetDownloadUrlAsync(id, ct));

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await assets.ListAsync(ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
        => Ok(new { deleted = await assets.DeleteAsync(id, ct) });
}
