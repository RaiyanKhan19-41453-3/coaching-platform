using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Communication;

public sealed record CreateAnnouncementRequest(string Title, string Body, string? TargetAudienceJson,
    AnnouncementPriority Priority, bool IsPinned, DateTimeOffset? ExpiresAt);

public sealed record AnnouncementResponse(Guid Id, string Title, string Body,
    AnnouncementPriority Priority, bool IsPinned, DateTimeOffset? PublishedAt);

public sealed record SendMessageRequest(Guid? ConversationId, Guid RecipientId, string Body);

public sealed record ConversationResponse(Guid Id, ConversationType Type, string? Title, DateTimeOffset? LastMessageAt);

public sealed record MessageResponse(Guid Id, Guid SenderId, string? Body, DateTimeOffset SentAt);

public sealed record UpdatePreferenceRequest(NotificationChannel Channel, string Type,
    bool IsEnabled, DigestFrequency DigestFrequency);

public sealed record NotificationResponse(Guid Id, string Type, string Title, string Body,
    bool IsRead, DateTimeOffset? SentAt);

public sealed record ListQuery(int Page = 1, int PageSize = 50);
