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
