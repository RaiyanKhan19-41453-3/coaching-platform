using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CoachingPlatform.Application.Common;

namespace CoachingPlatform.Infrastructure.Storage;

/// <summary>AWS S3 implementation of the storage abstraction. Presigned URLs are signed by the
/// AWS SDK with the real bucket host, so no endpoint rewriting is needed.</summary>
public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;

    public S3ObjectStorage(string region, string bucket, string? accessKey, string? secretKey)
    {
        _bucket = bucket;
        var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(region) };
        _client = string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(config)                       // IAM role / environment chain
            : new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        var buckets = await _client.ListBucketsAsync(ct);
        if (!buckets.Buckets.Any(b => b.BucketName == _bucket))
            await _client.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, ct);
    }

    public Task<string> PresignUploadAsync(string key, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult(_client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket, Key = key, Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry)
        }));

    public Task<string> PresignDownloadAsync(string key, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult(_client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket, Key = key, Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        }));

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try { await _client.GetObjectMetadataAsync(_bucket, key, ct); return true; }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return false; }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _client.DeleteObjectAsync(_bucket, key, ct);

    public void Dispose() => _client.Dispose();
}
