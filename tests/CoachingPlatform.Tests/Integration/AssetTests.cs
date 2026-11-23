using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Features.Content;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Content;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class AssetTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task RequestUpload_CreatesAssetAndPresignedUrl()
    {
        await NewTenantAsync();
        var result = await Sender.Send(new RequestAssetUpload(
            "notes.pdf", "application/pdf", 2048, "Lesson", null));

        var asset = await Db.Assets.SingleAsync(a => a.Id == result.Id);
        Assert.Equal(result.Key, asset.StorageKey);
        Assert.Equal(AssetKind.Pdf, asset.Kind);
        Assert.Equal(ScanStatus.Pending, asset.ScanStatus);
        Assert.Contains("storage.test/upload", result.Url);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ConfirmUpload_RequiresObjectThenMarksClean()
    {
        await NewTenantAsync();
        var uploaded = await Sender.Send(new RequestAssetUpload(
            "a.txt", "text/plain", 10, "Lesson", null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Sender.Send(new ConfirmAssetUpload(uploaded.Id)));

        Storage.Put(uploaded.Key);
        var url = await Sender.Send(new ConfirmAssetUpload(uploaded.Id));
        Assert.Contains("download", url);
        var asset = await Db.Assets.SingleAsync(a => a.Id == uploaded.Id);
        Assert.Equal(ScanStatus.Clean, asset.ScanStatus);
        Assert.Equal(url, asset.Url);
        Assert.Contains("download", await Sender.Send(new GetAssetDownloadUrl(uploaded.Id)));
    }

    [Fact]
    public async Task DeleteAsset_SoftDeletesAndRemovesObject()
    {
        await NewTenantAsync();
        var uploaded = await Sender.Send(new RequestAssetUpload(
            "b.txt", "text/plain", 10, "Lesson", null));
        Storage.Put(uploaded.Key);
        await Sender.Send(new ConfirmAssetUpload(uploaded.Id));

        Assert.True(await Sender.Send(new DeleteAsset(uploaded.Id)));
        Assert.Empty(await Db.Assets.ToListAsync());
        var stored = await Db.Assets.IgnoreQueryFilters().SingleAsync(a => a.Id == uploaded.Id);
        Assert.True(stored.IsDeleted);
        Assert.Empty(await Db.Assets.Where(a => a.Id == uploaded.Id).ToListAsync());
    }
}
