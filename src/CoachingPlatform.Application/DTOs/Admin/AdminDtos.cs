namespace CoachingPlatform.Application.DTOs.Admin;

public sealed record AuditLogResponse(Guid Id, string EntityName, string EntityId, string Action,
    string? ActorEmail, string? ChangesJson, DateTimeOffset OccurredAt);

public sealed record FeatureFlagRequest(string Key, string? Description, bool IsEnabled, string? TargetingJson);

public sealed record SearchResponse(IReadOnlyList<SearchItem> Students, IReadOnlyList<SearchItem> Courses,
    IReadOnlyList<SearchItem> Batches, IReadOnlyList<SearchItem> Assets);

public sealed record SearchItem(Guid Id, string Name);
