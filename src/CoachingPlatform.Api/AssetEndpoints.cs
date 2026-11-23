using CoachingPlatform.Application.Features.Content;
using MediatR;

namespace CoachingPlatform.Api;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssets(this IEndpointRouteBuilder app)
    {
        var assets = app.MapGroup("/api/assets").WithTags("Assets").RequireAuthorization();

        assets.MapPost("/upload-url", async (ISender sender, RequestAssetUpload cmd)
            => Results.Ok(await sender.Send(cmd)));
        assets.MapPost("/{id:guid}/complete", async (ISender sender, Guid id)
            => Results.Ok(new { url = await sender.Send(new ConfirmAssetUpload(id)) }));
        assets.MapGet("/{id:guid}/url", async (ISender sender, Guid id)
            => Results.Ok(new { url = await sender.Send(new GetAssetDownloadUrl(id)) }));
        assets.MapGet("/", async (ISender sender) => Results.Ok(await sender.Send(new ListAssets())));
        assets.MapDelete("/{id:guid}", async (ISender sender, Guid id)
            => Results.Ok(new { deleted = await sender.Send(new DeleteAsset(id)) }));

        return app;
    }
}
