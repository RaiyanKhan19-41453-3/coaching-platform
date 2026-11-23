using System.Text.Json;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Projections;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs.Handlers;

public sealed class EnrollmentCreatedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.EnrollmentCreated;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var batchId = payload.GetProperty("BatchId").GetGuid();
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
        await db.RefreshBatchUtilizationAsync(tenantId, batchId, ct);
    }
}

public sealed class AttendanceMarkedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.AttendanceMarked;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var batchId = payload.GetProperty("BatchId").GetGuid();
        var period = DateOnly.FromDateTime(payload.GetProperty("StartUtc").GetDateTimeOffset().UtcDateTime);
        var marked = payload.TryGetProperty("StudentIds", out var ids)
            ? ids.EnumerateArray().Select(x => x.GetGuid()).ToList()
            : [];
        var absent = payload.TryGetProperty("AbsentStudents", out var absentees)
            ? absentees.EnumerateArray().Select(x => x.GetGuid()).ToList()
            : [];

        if (absent.Count > 0)
        {
            var guardians = await db.StudentGuardians.IgnoreQueryFilters()
                .Where(g => absent.Contains(g.StudentId) && g.CanReceiveNotifications)
                .Select(g => new { g.StudentId, g.GuardianId }).ToListAsync(ct);

            foreach (var studentId in absent)
                db.Notifications.Add(Notice(tenantId, studentId, "You were marked absent", "Attendance notice"));
            foreach (var guardian in guardians)
                db.Notifications.Add(Notice(tenantId, guardian.GuardianId,
                    "Your child was marked absent", "Guardian attendance notice"));
        }

        foreach (var studentId in marked.Distinct())
            await db.RefreshAttendanceAsync(tenantId, studentId, batchId, period, ct);
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

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var studentId = payload.GetProperty("StudentId").GetGuid();
        var subjectId = payload.GetProperty("SubjectId").GetGuid();
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
        await db.RefreshGradesAsync(tenantId, studentId, subjectId, ct);
    }
}

public sealed class PaymentReceivedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.PaymentReceived;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
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
        var paidAt = payload.GetProperty("PaidAt").GetDateTimeOffset();
        await db.RefreshFeesAsync(tenantId, studentId, DateOnly.FromDateTime(paidAt.UtcDateTime), ct);
    }
}

public sealed class InvoiceCreatedHandler : IOutboxEventHandler
{
    public string EventType => EventTypes.InvoiceCreated;

    public async Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct)
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
        var period = DateOnly.Parse(payload.GetProperty("IssueDate").GetString()!);
        await db.RefreshFeesAsync(tenantId, studentId, period, ct);
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
