using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.DTOs.Identity;
using CoachingPlatform.Application.Features.Identity;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IAuthService
{
    Task<TenantProvisionedResponse> ProvisionTenantAsync(ProvisionTenantRequest request, CancellationToken ct = default);
    Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken ct = default);
}

public sealed class AuthService(ISender sender) : IAuthService
{
    public async Task<TenantProvisionedResponse> ProvisionTenantAsync(ProvisionTenantRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new ProvisionTenant(
            request.Name, request.Slug, request.AdminEmail, request.AdminPassword,
            request.AdminFirstName, request.AdminLastName, request.Subdomain), ct);
        return new TenantProvisionedResponse(id);
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new Login(request.Email, request.Password, request.TenantSlug), ct);
        return Map(result);
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshAccessToken(request.RefreshToken), ct);
        return Map(result);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrentUser(), ct);
        return new CurrentUserResponse(result.UserId, result.Email, result.FullName, result.TenantId, result.Roles);
    }

    private static AuthTokensResponse Map(AuthTokensDto source) => new(
        source.AccessToken, source.RefreshToken, source.ExpiresAt,
        source.UserId, source.TenantId, source.Email, source.Roles);
}
