namespace CoachingPlatform.Application.Common;

/// <summary>
/// Object-storage abstraction. The API only ever hands out presigned URLs — file bytes
/// never pass through the .NET process, so large uploads do not consume API resources.
/// </summary>
public interface IObjectStorage
{
    Task EnsureBucketAsync(CancellationToken ct = default);
    Task<string> PresignUploadAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task<string> PresignDownloadAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
