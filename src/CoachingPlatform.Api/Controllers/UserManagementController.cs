using CoachingPlatform.Application.DTOs.Identity;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class UserManagementController(IUserManagementService users) : ControllerBase
{
    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteUserRequest r, CancellationToken ct)
        => Ok(new { id = await users.InviteAsync(r, ct) });
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await users.ListAsync(new UserQuery(search, page, pageSize), ct));
    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole([FromRoute] Guid id, [FromBody] AssignRoleRequest r, CancellationToken ct)
        => Ok(new { assigned = await users.AssignRoleAsync(id, r, ct) });
    [HttpDelete("{id:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole([FromRoute] Guid id, [FromRoute] string role, CancellationToken ct)
        => Ok(new { removed = await users.RemoveRoleAsync(id, role, ct) });
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus([FromRoute] Guid id, [FromBody] SetUserStatusRequest r, CancellationToken ct)
        => Ok(new { updated = await users.SetStatusAsync(id, r, ct) });
}
