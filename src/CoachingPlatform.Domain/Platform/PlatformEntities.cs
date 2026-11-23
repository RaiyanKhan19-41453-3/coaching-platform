using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Domain.Platform;

/// <summary>Append-only audit trail. Written by a SaveChanges interceptor.</summary>
public sealed class AuditLog : Entity
{
    public Guid? TenantId { get; set; }
    public string EntityName { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string Action { get; set; } = default!;     // Created / Updated / Deleted
    public Guid? ActorId { get; set; }
    public string? ActorEmail { get; set; }
    public string? ChangesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>Transactional outbox: domain events are committed with the data, then dispatched.</summary>
public sealed class OutboxMessage : Entity
{
    public Guid? TenantId { get; set; }
    public string EventType { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
