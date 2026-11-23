using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Content;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Content;

public sealed record RequestAssetUpload(
    string FileName, string ContentType, long SizeBytes,
    string? OwnerType, Guid? OwnerId, bool IsPublic = false) : IRequest<AssetUploadDto>, IRequiresTenant;

public sealed class RequestAssetUploadValidator : AbstractValidator<RequestAssetUpload>
{
    public RequestAssetUploadValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SizeBytes).InclusiveBetween(1L, 2L * 1024 * 1024 * 1024);
    }
}

public sealed record AssetUploadDto(Guid Id, string Key, string Url, DateTimeOffset ExpiresAt);

public sealed class RequestAssetUploadHandler(IApplicationDbContext db, ICurrentUser current, IObjectStorage storage)
    : IRequestHandler<RequestAssetUpload, AssetUploadDto>
{
    public async Task<AssetUploadDto> Handle(RequestAssetUpload request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var safeName = Path.GetFileName(request.FileName).Replace(' ', '_');
        var key = $"{current.TenantId:N}/{now:yyyy/MM}/{Guid.NewGuid():N}-{safeName}";
        var expires = now.AddMinutes(15);

        var asset = new Asset
        {
            TenantId = current.TenantId,
            FileName = safeName,
            StorageKey = key,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            Kind = MapKind(request.ContentType),
            ScanStatus = ScanStatus.Pending,
            IsPublic = request.IsPublic,
            OwnerType = request.OwnerType,
            OwnerId = request.OwnerId,
            UploadedBy = current.UserId,
            ExpiresAt = expires
        };
        db.Set<Asset>().Add(asset);
        await db.SaveChangesAsync(ct);

        var url = await storage.PresignUploadAsync(key, expires - now, ct);
        return new AssetUploadDto(asset.Id, key, url, expires);
    }

    private static AssetKind MapKind(string contentType) => contentType.ToLowerInvariant() switch
    {
        var ct when ct.StartsWith("image/") => AssetKind.Image,
        "application/pdf" => AssetKind.Pdf,
        var ct when ct.StartsWith("video/") => AssetKind.Video,
        var ct when ct.StartsWith("audio/") => AssetKind.Audio,
        var ct when ct.Contains("word") || ct.Contains("sheet") || ct.Contains("document") => AssetKind.Document,
        _ => AssetKind.Other
    };
}

public sealed record ConfirmAssetUpload(Guid AssetId) : IRequest<string>, IRequiresTenant;

public sealed class ConfirmAssetUploadHandler(IApplicationDbContext db, IObjectStorage storage)
    : IRequestHandler<ConfirmAssetUpload, string>
{
    public async Task<string> Handle(ConfirmAssetUpload request, CancellationToken ct)
    {
        var asset = await db.Set<Asset>().FirstOrDefaultAsync(a => a.Id == request.AssetId, ct)
                    ?? throw new NotFoundException(nameof(Asset), request.AssetId);
        if (!await storage.ExistsAsync(asset.StorageKey, ct))
            throw new ConflictException("Object was not found in storage. Upload it first.");

        asset.ScanStatus = ScanStatus.Clean;   // virus scanning can be added as a worker step
        var url = await storage.PresignDownloadAsync(asset.StorageKey, TimeSpan.FromDays(7), ct);
        asset.Url = url;
        await db.SaveChangesAsync(ct);
        return url;
    }
}

public sealed record GetAssetDownloadUrl(Guid AssetId) : IRequest<string>, IRequiresTenant;

public sealed class GetAssetDownloadUrlHandler(IApplicationDbContext db, IObjectStorage storage)
    : IRequestHandler<GetAssetDownloadUrl, string>
{
    public async Task<string> Handle(GetAssetDownloadUrl request, CancellationToken ct)
    {
        var asset = await db.Set<Asset>().FirstOrDefaultAsync(a => a.Id == request.AssetId, ct)
                    ?? throw new NotFoundException(nameof(Asset), request.AssetId);
        if (asset.ScanStatus != ScanStatus.Clean)
            throw new ConflictException("Asset has not been confirmed after upload.");
        return await storage.PresignDownloadAsync(asset.StorageKey, TimeSpan.FromHours(1), ct);
    }
}

public sealed record DeleteAsset(Guid AssetId) : IRequest<bool>, IRequiresTenant;

public sealed class DeleteAssetHandler(IApplicationDbContext db, IObjectStorage storage)
    : IRequestHandler<DeleteAsset, bool>
{
    public async Task<bool> Handle(DeleteAsset request, CancellationToken ct)
    {
        var asset = await db.Set<Asset>().FirstOrDefaultAsync(a => a.Id == request.AssetId, ct)
                    ?? throw new NotFoundException(nameof(Asset), request.AssetId);
        await storage.RemoveAsync(asset.StorageKey, ct);
        asset.IsDeleted = true;
        asset.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record ListAssets() : IRequest<IReadOnlyList<AssetDto>>, IRequiresTenant;

public sealed record AssetDto(Guid Id, string FileName, string ContentType, long SizeBytes,
    AssetKind Kind, ScanStatus ScanStatus, bool IsPublic, DateTimeOffset CreatedAt);

public sealed class ListAssetsHandler(IApplicationDbContext db) : IRequestHandler<ListAssets, IReadOnlyList<AssetDto>>
{
    public async Task<IReadOnlyList<AssetDto>> Handle(ListAssets request, CancellationToken ct)
        => await db.Set<Asset>().AsNoTracking().OrderByDescending(a => a.CreatedAt)
            .Select(a => new AssetDto(a.Id, a.FileName, a.ContentType, a.SizeBytes,
                a.Kind, a.ScanStatus, a.IsPublic, a.CreatedAt)).ToListAsync(ct);
}
