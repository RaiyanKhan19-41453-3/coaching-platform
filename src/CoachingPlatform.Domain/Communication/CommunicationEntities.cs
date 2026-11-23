using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Domain.Communication;

public sealed class Announcement : TenantEntity
{
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? TargetAudienceJson { get; set; } // roles / batches / specific users
    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;
    public bool IsPinned { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? AttachmentStorageKey { get; set; }
    public ICollection<Notification> Notifications { get; set; } = [];
}

/// <summary>One delivery attempt on one channel. Durable outbox workers create these.</summary>
public sealed class Notification : TenantEntity
{
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public string Type { get; set; } = default!;         // "attendance.absent", "fee.due"
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? DataJson { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int RetryCount { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class NotificationPreference : TenantEntity
{
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Type { get; set; } = default!;        // "*" for channel-wide default
    public bool IsEnabled { get; set; } = true;
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }
    public DigestFrequency DigestFrequency { get; set; } = DigestFrequency.Immediate;
}

public sealed class Conversation : TenantEntity
{
    public ConversationType Type { get; set; } = ConversationType.Direct;
    public string? Title { get; set; }
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public Guid CreatorId { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}

public sealed class ConversationParticipant : TenantEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = default!;
    public Guid UserId { get; set; }
    public ParticipantRole Role { get; set; } = ParticipantRole.Member;
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public bool IsMuted { get; set; }
}

public sealed class Message : TenantEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = default!;
    public Guid SenderId { get; set; }
    public string? Body { get; set; }
    public string? AttachmentStorageKey { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
}

public sealed class MessageReceipt : TenantEntity
{
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = default!;
    public Guid UserId { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
