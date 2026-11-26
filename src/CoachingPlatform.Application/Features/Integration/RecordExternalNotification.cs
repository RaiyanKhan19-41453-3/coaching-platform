using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Integration;

/// <summary>
/// Applies an inbound integration event once. When the producer supplies a stable
/// <paramref name="EventId"/>, the inbox key (event + recipient) makes this idempotent
/// regardless of payload or how much time has passed. Calls without an event id keep
/// the original best-effort time-window check for backwards compatibility.
/// </summary>
public sealed record RecordExternalNotification(Guid TenantId, Guid UserId, string Type,
    string Title, string Body, Guid? EventId = null) : IRequest<Guid>;

public sealed class RecordExternalNotificationHandler(IApplicationDbContext db) : IRequestHandler<RecordExternalNotification, Guid>
{
    public async Task<Guid> Handle(RecordExternalNotification request, CancellationToken ct)
    {
        // One event can target many candidates, so the recipient is part of the key.
        var eventKey = request.EventId.HasValue
            ? $"{request.EventId.Value:N}:{request.UserId:N}"
            : null;

        if (eventKey is not null)
        {
            var recorded = await db.Set<InboxMessage>().FirstOrDefaultAsync(i =>
                i.TenantId == request.TenantId && i.EventType == request.Type &&
                i.EventId == eventKey, ct);
            if (recorded is not null) return recorded.EffectId;
        }
        else
        {
            var existing = await db.Set<Notification>().FirstOrDefaultAsync(n =>
                n.UserId == request.UserId && n.Type == request.Type && n.Body == request.Body &&
                n.CreatedAt > DateTimeOffset.UtcNow.AddHours(-1), ct);
            if (existing is not null) return existing.Id;
        }

        var now = DateTimeOffset.UtcNow;
        var notice = new Notification
        {
            TenantId = request.TenantId, UserId = request.UserId,
            Channel = NotificationChannel.InApp, Type = request.Type,
            Title = request.Title, Body = request.Body, SentAt = now
        };
        db.Set<Notification>().Add(notice);

        if (eventKey is not null)
        {
            // Unique index on (tenant, type, event id) is the final guard. If a concurrent
            // delivery wins the race, its failure simply triggers a producer retry, which
            // will then be short-circuited by the inbox lookup above.
            db.Set<InboxMessage>().Add(new InboxMessage
            {
                TenantId = request.TenantId,
                EventType = request.Type,
                EventId = eventKey,
                ReceivedAt = now,
                ProcessedAt = now,
                EffectId = notice.Id
            });
        }

        await db.SaveChangesAsync(ct);
        return notice.Id;
    }
}
