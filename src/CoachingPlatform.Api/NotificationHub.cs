using Microsoft.AspNetCore.SignalR;

namespace CoachingPlatform.Api;

/// <summary>
/// Real-time channel for notifications, attendance updates and chat. Scale out by adding a
/// Redis backplane: builder.Services.AddSignalR().AddStackExchangeRedis(...).
/// </summary>
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        await base.OnDisconnectedAsync(exception);
    }
}
