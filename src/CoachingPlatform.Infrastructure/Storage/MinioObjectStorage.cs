using CoachingPlatform.Application.Common;
using Minio;
using Minio.DataModel.Args;

namespace CoachingPlatform.Infrastructure.Storage;

/// <summary>
/// MinIO/S3 adapter. Server-side calls use the container-network endpoint; presigned URLs
/// are rewritten to a browser-reachable public endpoint (e.g. http://localhost:9000).
/// </summary>
public sealed class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient _client;
    private readonly IMinioClient _signingClient;
    private readonly string _bucket;

    public MinioObjectStorage(string endpoint, string accessKey, string secretKey,
        string bucket, string publicEndpoint, bool useSsl = false)
    {
        _bucket = bucket;
        _client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSsl)
            .Build();

        // SigV4 signs the Host header, so presigning must use the browser-reachable host.
        // Presigning is a local computation — this client never makes a network call.
        var publicUri = new Uri(publicEndpoint.TrimEnd('/'));
        _signingClient = new MinioClient()
            .WithEndpoint(publicUri.Authority)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(publicUri.Scheme == Uri.UriSchemeHttps)
            .Build();
    }

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        if (await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket), ct)) return;
        await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), ct);
    }

    public async Task<string> PresignUploadAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        return await _signingClient.PresignedPutObjectAsync(
            new PresignedPutObjectArgs().WithBucket(_bucket).WithObject(key).WithExpiry((int)expiry.TotalSeconds));
    }

    public async Task<string> PresignDownloadAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        return await _signingClient.PresignedGetObjectAsync(
            new PresignedGetObjectArgs().WithBucket(_bucket).WithObject(key).WithExpiry((int)expiry.TotalSeconds));
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_bucket).WithObject(key), ct);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucket).WithObject(key), ct);

}
