using System.Text.Json;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs;

/// <summary>
/// Drains the transactional outbox in small batches. A failed message is retried with
/// backoff and dead-lettered after 5 attempts; nothing is silently dropped.
/// </summary>
public sealed class OutboxDispatcher(
    JobDbContextFactory factory,
    IEnumerable<IOutboxEventHandler> handlers,
    ILogger<OutboxDispatcher> logger)
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    public async Task DispatchAsync() => await DispatchCoreAsync(CancellationToken.None);

    public async Task DispatchCoreAsync(CancellationToken ct)
    {
        var byType = handlers.ToDictionary(h => h.EventType, StringComparer.Ordinal);
        await using var scan = factory.Create(Guid.Empty);
        var ids = await scan.OutboxMessages.IgnoreQueryFilters()
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredAt).Take(BatchSize)
            .Select(m => m.Id).ToListAsync(ct);

        foreach (var id in ids)
        {
            // One context per message: notifications and the ProcessedAt stamp commit atomically.
            var ambient = new StaticTenantContext { IsSystem = true, UserEmail = "system@coaching-platform" };
            await using var db = factory.CreateFor(ambient);
            var message = await db.OutboxMessages.IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id, ct);
            if (message is null || message.ProcessedAt.HasValue) continue;

            ambient.TenantId = message.TenantId ?? Guid.Empty;
            try
            {
                using var payload = JsonDocument.Parse(message.PayloadJson);
                if (byType.TryGetValue(message.EventType, out var handler))
                    await handler.HandleAsync(payload.RootElement, db, ambient.TenantId, ct);
                else logger.LogWarning("No handler for outbox event {EventType}", message.EventType);

                message.ProcessedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox event {EventType} ({Id}) failed", message.EventType, id);
                message.RetryCount++;
                message.LastError = ex.Message[..Math.Min(ex.Message.Length, 1000)];
                message.ProcessedAt = message.RetryCount >= MaxRetries ? DateTimeOffset.UtcNow : null;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
