using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Features.Communication;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Infrastructure.Persistence;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs;

/// <summary>Drains announcement fan-out events in batches so large audiences never block a request.</summary>
public sealed class AnnouncementFanoutWorker(IServiceScopeFactory scopes, ILogger<AnnouncementFanoutWorker> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task DispatchAsync()
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var events = await db.OutboxMessages.IgnoreQueryFilters()
            .Where(m => m.EventType == EventTypes.AnnouncementCreated && m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredAt).Take(20).ToListAsync();
        foreach (var ev in events)
        {
            try
            {
                using var doc = JsonDocument.Parse(ev.PayloadJson);
                var announcementId = doc.RootElement.GetProperty("Id").GetGuid();
                await sender.Send(new FanOutAnnouncement(announcementId));
                ev.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                ev.RetryCount++;
                ev.LastError = ex.Message[..Math.Min(ex.Message.Length, 500)];
            }
        }
        if (events.Count > 0) await db.SaveChangesAsync();
        if (events.Count > 0) logger.LogInformation("Processed {Count} announcement fan-out events", events.Count);
    }
}
