using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CoachingPlatform.Api;

/// <summary>
/// Authenticated real-time channel. Clients are placed in a per-user group so the outbox
/// dispatcher can push notifications only to the intended recipient. The Redis backplane
/// (configured in Program.cs when Redis is available) fans messages out across API replicas.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst("nameid")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst("nameid")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnDisconnectedAsync(exception);
    }
}
