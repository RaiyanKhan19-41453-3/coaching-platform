using System.Text.Json;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs.Handlers;

public sealed class EnrollmentCreatedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.EnrollmentCreated;

    public Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var batchName = payload.GetProperty("Name").GetString();
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = studentId,
            Channel = NotificationChannel.InApp,
            Type = EventType,
            Title = "Enrollment confirmed",
            Body = $"You are enrolled in {batchName}.",
            SentAt = DateTimeOffset.UtcNow
        });
        return Task.CompletedTask;
    }
}

public sealed class AttendanceMarkedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.AttendanceMarked;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        if (!payload.TryGetProperty("AbsentStudents", out var absentees)) return;
        var studentIds = absentees.EnumerateArray().Select(x => x.GetGuid()).ToList();
        if (studentIds.Count == 0) return;

        var guardians = await db.StudentGuardians.IgnoreQueryFilters()
            .Where(g => studentIds.Contains(g.StudentId) && g.CanReceiveNotifications)
            .Select(g => new { g.StudentId, g.GuardianId }).ToListAsync(ct);

        foreach (var studentId in studentIds)
            db.Notifications.Add(Notice(tenantId, studentId, "You were marked absent", "Attendance notice"));

        foreach (var guardian in guardians)
            db.Notifications.Add(Notice(tenantId, guardian.GuardianId,
                "Your child was marked absent", "Guardian attendance notice"));
    }

    private static Notification Notice(Guid tenantId, Guid userId, string body, string type) => new()
    {
        TenantId = tenantId,
        UserId = userId,
        Channel = NotificationChannel.InApp,
        Type = type,
        Title = "Attendance",
        Body = body,
        SentAt = DateTimeOffset.UtcNow
    };
}

public sealed class AssessmentSubmittedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.AssessmentSubmitted;

    public Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var score = payload.GetProperty("Score").GetDecimal();
        var max = payload.GetProperty("MaxScore").GetDecimal();
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = studentId,
            Channel = NotificationChannel.InApp,
            Type = EventType,
            Title = "Assessment submitted",
            Body = $"Your score: {score:0.##} / {max:0.##}",
            SentAt = DateTimeOffset.UtcNow
        });
        return Task.CompletedTask;
    }
}

public sealed class PaymentReceivedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.PaymentReceived;

    public Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var amount = payload.GetProperty("Amount").GetDecimal();
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = studentId,
            Channel = NotificationChannel.InApp,
            Type = EventType,
            Title = "Payment received",
            Body = $"We received {amount:N2}. Thank you.",
            SentAt = DateTimeOffset.UtcNow
        });
        return Task.CompletedTask;
    }
}

public sealed class InvoiceCreatedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.InvoiceCreated;

    public Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var number = payload.GetProperty("InvoiceNumber").GetString();
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = studentId,
            Channel = NotificationChannel.InApp,
            Type = EventType,
            Title = "New invoice",
            Body = $"Invoice {number} has been issued.",
            SentAt = DateTimeOffset.UtcNow
        });
        return Task.CompletedTask;
    }
}

public sealed class TenantProvisionedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.TenantProvisioned;

    public Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var adminId = payload.GetProperty("AdminUserId").GetGuid();
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = adminId,
            Channel = NotificationChannel.InApp,
            Type = EventType,
            Title = "Welcome",
            Body = "Your coaching center is ready.",
            SentAt = DateTimeOffset.UtcNow
        });
        return Task.CompletedTask;
    }
}
