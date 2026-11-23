using CoachingPlatform.Application.DTOs.Content;
using CoachingPlatform.Application.Features.Content;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IAssetService
{
    Task<AssetUploadResponse> RequestUploadAsync(RequestUploadRequest request, CancellationToken ct = default);
    Task<DownloadUrlResponse> ConfirmUploadAsync(Guid assetId, CancellationToken ct = default);
    Task<DownloadUrlResponse> GetDownloadUrlAsync(Guid assetId, CancellationToken ct = default);
    Task<IReadOnlyList<AssetResponse>> ListAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid assetId, CancellationToken ct = default);
}

public sealed class AssetService(ISender sender) : IAssetService
{
    public async Task<AssetUploadResponse> RequestUploadAsync(RequestUploadRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RequestAssetUpload(request.FileName, request.ContentType,
            request.SizeBytes, request.OwnerType, request.OwnerId, request.IsPublic), ct);
        return new AssetUploadResponse(result.Id, result.Key, result.Url, result.ExpiresAt);
    }

    public async Task<DownloadUrlResponse> ConfirmUploadAsync(Guid assetId, CancellationToken ct)
        => new(await sender.Send(new ConfirmAssetUpload(assetId), ct));

    public async Task<DownloadUrlResponse> GetDownloadUrlAsync(Guid assetId, CancellationToken ct)
        => new(await sender.Send(new GetAssetDownloadUrl(assetId), ct));

    public async Task<IReadOnlyList<AssetResponse>> ListAsync(CancellationToken ct)
    {
        var rows = await sender.Send(new ListAssets(), ct);
        return rows.Select(r => new AssetResponse(r.Id, r.FileName, r.ContentType, r.SizeBytes,
            r.Kind, r.ScanStatus, r.IsPublic, r.CreatedAt)).ToList();
    }

    public Task<bool> DeleteAsync(Guid assetId, CancellationToken ct)
        => sender.Send(new DeleteAsset(assetId), ct);
}
