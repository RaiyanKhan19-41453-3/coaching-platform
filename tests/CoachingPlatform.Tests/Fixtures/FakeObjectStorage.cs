using CoachingPlatform.Application.Common;

namespace CoachingPlatform.Tests.Fixtures;

public sealed class FakeObjectStorage : IObjectStorage
{
    private readonly HashSet<string> _objects = [];

    public Task EnsureBucketAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<string> PresignUploadAsync(string key, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://storage.test/upload?key={Uri.EscapeDataString(key)}");

    public Task<string> PresignDownloadAsync(string key, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://storage.test/download?key={Uri.EscapeDataString(key)}");

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_objects.Contains(key));

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _objects.Remove(key);
        return Task.CompletedTask;
    }

    public void Put(string key) => _objects.Add(key);
}
