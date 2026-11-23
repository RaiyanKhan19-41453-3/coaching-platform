using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Content;

public sealed record RequestUploadRequest(
    string FileName, string ContentType, long SizeBytes,
    string? OwnerType, Guid? OwnerId, bool IsPublic = false);

public sealed record AssetUploadResponse(Guid Id, string Key, string Url, DateTimeOffset ExpiresAt);

public sealed record AssetResponse(
    Guid Id, string FileName, string ContentType, long SizeBytes,
    AssetKind Kind, ScanStatus ScanStatus, bool IsPublic, DateTimeOffset CreatedAt);

public sealed record DownloadUrlResponse(string Url);

public sealed record AttachMaterialRequest(Guid? BatchId, Guid? TopicId, string Title,
    MaterialType Type, Guid? AssetId, string? ExternalUrl, int? DurationMinutes, int Order);

public sealed record MaterialResponse(Guid Id, string Title, MaterialType Type,
    string? StorageKey, string? ExternalUrl, int? DurationMinutes, bool IsPublished);

public sealed record PublishMaterialRequest(bool Publish);
