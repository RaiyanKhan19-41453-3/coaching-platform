using CoachingPlatform.Application.DTOs.Communication;
using CoachingPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api/communication")]
[Authorize]
[Produces("application/json")]
public sealed class CommunicationController(ICommunicationService communication) : ControllerBase
{
    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest request, CancellationToken ct)
        => Ok(await communication.CreateAnnouncementAsync(request, ct));

    [HttpGet("announcements")]
    public async Task<IActionResult> ListAnnouncements([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await communication.ListAnnouncementsAsync(page, pageSize, ct));

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
        => Ok(await communication.SendMessageAsync(request, ct));

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(CancellationToken ct)
        => Ok(await communication.ListConversationsAsync(ct));

    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<IActionResult> Messages([FromRoute] Guid id, CancellationToken ct)
        => Ok(await communication.GetMessagesAsync(id, ct));

    [HttpPost("conversations/{id:guid}/read")]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id, CancellationToken ct)
        => Ok(new { read = await communication.MarkConversationReadAsync(id, ct) });

    [HttpPut("notification-preferences")]
    public async Task<IActionResult> UpdatePreference([FromBody] UpdatePreferenceRequest request, CancellationToken ct)
        => Ok(new { updated = await communication.UpdatePreferenceAsync(request, ct) });

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await communication.ListNotificationsAsync(page, pageSize, ct));

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead([FromRoute] Guid id, CancellationToken ct)
        => Ok(new { read = await communication.MarkNotificationReadAsync(id, ct) });
}
