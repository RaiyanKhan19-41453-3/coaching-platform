using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Domain.Content;

/// <summary>Object-storage manifest. Actual bytes live in S3/Blob/MinIO, never in Postgres.</summary>
public sealed class Asset : TenantEntity
{
    public string FileName { get; set; } = default!;
    public string StorageKey { get; set; } = default!;
    public string? Url { get; set; }
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public AssetKind Kind { get; set; } = AssetKind.Other;
    public ScanStatus ScanStatus { get; set; } = ScanStatus.Pending;
    public string? Checksum { get; set; }
    public bool IsPublic { get; set; }
    public Guid? UploadedBy { get; set; }
    public string? OwnerType { get; set; }       // polymorphic: "Lesson", "Invoice", "User"
    public Guid? OwnerId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
