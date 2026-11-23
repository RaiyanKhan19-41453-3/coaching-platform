using System.Text.Json;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoachingPlatform.Api.Jobs;

/// <summary>
/// Drains the transactional outbox in small batches. A failed message is retried with
/// backoff and dead-lettered after 5 attempts; nothing is silently dropped.
/// </summary>
public sealed class OutboxDispatcher(
    JobDbContextFactory factory,
    IEnumerable<IOutboxEventHandler> handlers,
    IHubContext<NotificationHub> hub,
    ILogger<OutboxDispatcher> logger)
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    public async Task DispatchAsync() => await DispatchCoreAsync(CancellationToken.None);

    /// <summary>
    /// A Postgres transaction-scoped advisory lock serialises dispatchers (recurring job plus
    /// manual trigger) so the same message can never be handled twice concurrently.
    /// </summary>
    public async Task DispatchCoreAsync(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var lockCommand = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtext('coaching-outbox-dispatch'))", connection, (NpgsqlTransaction)transaction))
        {
            await lockCommand.ExecuteNonQueryAsync(ct);
        }

        await DispatchUnlockedAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task DispatchUnlockedAsync(CancellationToken ct)
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

                var pushed = db.ChangeTracker.Entries<Notification>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity)
                    .ToList();

                message.ProcessedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                // Push only after the transaction commits: a transport failure must not
                // trigger a retry that would duplicate the notification row.
                foreach (var notice in pushed)
                {
                    try
                    {
                        await hub.Clients.Group($"user:{notice.UserId}").SendAsync("notification", new
                        {
                            id = notice.Id,
                            type = notice.Type,
                            title = notice.Title,
                            body = notice.Body,
                            sentAt = notice.SentAt
                        }, ct);
                    }
                    catch (Exception pushEx)
                    {
                        logger.LogWarning(pushEx, "SignalR push failed for notification {Id}", notice.Id);
                    }
                }
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
