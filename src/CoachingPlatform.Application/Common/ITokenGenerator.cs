namespace CoachingPlatform.Application.Common;

public sealed record GeneratedToken(string AccessToken, DateTimeOffset ExpiresAt, string JwtId);

public interface ITokenGenerator
{
    GeneratedToken Generate(Guid userId, Guid tenantId, string email, IReadOnlyList<string> roles);
}

public sealed record AuthTokensDto(
    string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt,
    Guid UserId, Guid TenantId, string Email, IReadOnlyList<string> Roles);
