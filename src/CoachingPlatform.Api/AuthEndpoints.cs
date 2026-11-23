using CoachingPlatform.Application.Features.Identity;
using MediatR;

namespace CoachingPlatform.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/register-tenant", async (ISender sender, ProvisionTenant cmd)
            => Results.Ok(new { tenantId = await sender.Send(cmd) }))
            .WithSummary("Provision a new coaching center with its admin account");

        auth.MapPost("/login", async (ISender sender, Login cmd)
            => Results.Ok(await sender.Send(cmd)));

        auth.MapPost("/refresh", async (ISender sender, RefreshAccessToken cmd)
            => Results.Ok(await sender.Send(cmd)));

        // Authenticated route lives in the protected group below (see ApiEndpoints).
        return app;
    }
}
