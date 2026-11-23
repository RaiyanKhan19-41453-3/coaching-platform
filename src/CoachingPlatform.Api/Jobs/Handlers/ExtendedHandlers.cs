using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Projections;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs.Handlers;

public sealed class RefundHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.PaymentRefunded;

    public Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId, UserId = payload.GetProperty("StudentId").GetGuid(),
            Channel = NotificationChannel.InApp, Type = EventType,
            Title = "Payment refunded", Body = $"A refund of {payload.GetProperty("Amount").GetDecimal():N2} was processed.",
            SentAt = DateTimeOffset.UtcNow
        });
        return Task.CompletedTask;
    }
}

public sealed class EnrollmentStatusChangedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.EnrollmentStatusChanged;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var batchId = payload.GetProperty("BatchId").GetGuid();
        var status = payload.GetProperty("Status").GetString();
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId, UserId = studentId, Channel = NotificationChannel.InApp,
            Type = EventType, Title = "Enrollment updated", Body = $"Enrollment status is now {status}.",
            SentAt = DateTimeOffset.UtcNow
        });
        await db.RefreshBatchUtilizationAsync(tenantId, batchId, ct);
    }
}

public sealed class AssessmentGradedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.AssessmentGraded;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var assessmentId = payload.GetProperty("AssessmentId").GetGuid();
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId, UserId = studentId, Channel = NotificationChannel.InApp,
            Type = EventType, Title = "Result published",
            Body = $"Your score is {payload.GetProperty("Score").GetDecimal():0.##}.",
            SentAt = DateTimeOffset.UtcNow
        });
        var subjectId = await db.Set<Domain.Assessment.Assessment>()
            .Where(a => a.Id == assessmentId).Select(a => a.SubjectId).FirstOrDefaultAsync(ct);
        if (subjectId != Guid.Empty) await db.RefreshGradesAsync(tenantId, studentId, subjectId, ct);
    }
}

/// <summary>Delivers persisted notifications through email/SMS according to preferences.</summary>
public sealed class NotificationDeliveryHandler(IMessageTransport transport, ILogger<NotificationDeliveryHandler> logger)
    : IOutboxEventHandler
{
    public string EventType => EventTypes.NotificationCreated;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var userId = payload.GetProperty("UserId").GetGuid();
        var type = payload.GetProperty("Type").GetString() ?? "notification";
        var title = payload.GetProperty("Title").GetString() ?? "Notification";
        var body = payload.TryGetProperty("Body", out var bodyElement)
            ? bodyElement.GetString() ?? title : title;

        var user = await db.Set<Domain.Identity.User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return;

        var preferences = await db.Set<NotificationPreference>()
            .Where(p => p.UserId == userId && (p.Type == type || p.Type == "*")).ToListAsync(ct);

        var emailPrefs = preferences.Where(p => p.Channel == NotificationChannel.Email).ToList();
        if (emailPrefs.Count == 0 || emailPrefs.Any(p => p.IsEnabled))
            await transport.SendEmailAsync(user.Email, title, body, ct);

        var phone = user.PhoneNumber;
        var smsPrefs = preferences.Where(p => p.Channel == NotificationChannel.Sms).ToList();
        if (!string.IsNullOrWhiteSpace(phone) && (smsPrefs.Count == 0 || smsPrefs.Any(p => p.IsEnabled)))
            await transport.SendSmsAsync(phone, body, ct);

        logger.LogDebug("Delivered notification {Type} to {UserId}", type, userId);
    }
}
