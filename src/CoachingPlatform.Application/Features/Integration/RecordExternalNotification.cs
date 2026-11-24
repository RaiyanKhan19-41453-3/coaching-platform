using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Integration;

public sealed record RecordExternalNotification(Guid TenantId, Guid UserId, string Type,
    string Title, string Body) : IRequest<Guid>;

public sealed class RecordExternalNotificationHandler(IApplicationDbContext db) : IRequestHandler<RecordExternalNotification, Guid>
{
    public async Task<Guid> Handle(RecordExternalNotification request, CancellationToken ct)
    {
        // Webhook retries must not create duplicate notifications.
        var existing = await db.Set<Notification>().FirstOrDefaultAsync(n =>
            n.UserId == request.UserId && n.Type == request.Type && n.Body == request.Body &&
            n.CreatedAt > DateTimeOffset.UtcNow.AddHours(-1), ct);
        if (existing is not null) return existing.Id;

        var notice = new Notification
        {
            TenantId = request.TenantId, UserId = request.UserId,
            Channel = NotificationChannel.InApp, Type = request.Type,
            Title = request.Title, Body = request.Body, SentAt = DateTimeOffset.UtcNow
        };
        db.Set<Notification>().Add(notice);
        await db.SaveChangesAsync(ct);
        return notice.Id;
    }
}
