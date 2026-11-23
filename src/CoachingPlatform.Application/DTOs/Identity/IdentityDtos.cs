using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Identity;

public sealed record ProvisionTenantRequest(
    string Name, string Slug, string AdminEmail, string AdminPassword,
    string AdminFirstName, string AdminLastName, string? Subdomain);

public sealed record TenantProvisionedResponse(Guid TenantId);

public sealed record LoginRequest(string Email, string Password, string? TenantSlug);

public sealed record AuthTokensResponse(
    string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt,
    Guid UserId, Guid TenantId, string Email, IReadOnlyList<string> Roles);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record CurrentUserResponse(
    Guid UserId, string Email, string FullName, Guid TenantId, IReadOnlyList<string> Roles);
