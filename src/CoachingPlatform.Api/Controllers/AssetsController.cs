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
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> RequestUpload([FromBody] RequestUploadRequest request, CancellationToken ct)
        => Ok(await assets.RequestUploadAsync(request, ct));

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken ct)
        => Ok(await assets.ConfirmUploadAsync(id, ct));

    [HttpGet("{id:guid}/url")]
    public async Task<IActionResult> DownloadUrl([FromRoute] Guid id, CancellationToken ct)
        => Ok(await assets.GetDownloadUrlAsync(id, ct));

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await assets.ListAsync(ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
        => Ok(new { deleted = await assets.DeleteAsync(id, ct) });

    [HttpPost("materials")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> AttachMaterial([FromBody] AttachMaterialRequest request, CancellationToken ct)
        => Ok(await assets.AttachMaterialAsync(request, ct));

    [HttpGet("materials")]
    public async Task<IActionResult> ListMaterials([FromQuery] Guid? batchId, [FromQuery] Guid? topicId, CancellationToken ct)
        => Ok(await assets.ListMaterialsAsync(batchId, topicId, ct));

    [HttpPost("materials/{materialId:guid}/publish")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> PublishMaterial([FromRoute] Guid materialId,
        [FromBody] PublishMaterialRequest request, CancellationToken ct)
        => Ok(new { published = await assets.PublishMaterialAsync(materialId, request, ct) });
}
