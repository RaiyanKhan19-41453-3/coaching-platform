using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Identity;

namespace CoachingPlatform.Application.Features.Identity;

/// <summary>Shared token issuance: access token + rotated refresh token, committed in one save.</summary>
public sealed class TokenIssuer(IApplicationDbContext db, ITokenGenerator tokens)
{
    public async Task<AuthTokensDto> IssueAsync(
        User user, Guid tenantId, IReadOnlyList<string> roles, CancellationToken ct)
    {
        var access = tokens.Generate(user.Id, tenantId, user.Email, roles);
        var refresh = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        db.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14)
        });
        await db.SaveChangesAsync(ct);
        return new AuthTokensDto(access.AccessToken, refresh, access.ExpiresAt, user.Id, tenantId, user.Email, roles);
    }
}
