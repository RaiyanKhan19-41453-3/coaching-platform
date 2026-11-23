using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Identity;

public sealed record RefreshAccessToken(string RefreshToken) : IRequest<AuthTokensDto>;

public sealed class RefreshAccessTokenValidator : AbstractValidator<RefreshAccessToken>
{
    public RefreshAccessTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshAccessTokenHandler(
    IApplicationDbContext db, TokenIssuer issuer)
    : IRequestHandler<RefreshAccessToken, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RefreshAccessToken request, CancellationToken ct)
    {
        var hash = TokenHasher.Sha256(request.RefreshToken);
        var stored = await db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
                     ?? throw new AppException("Invalid refresh token.", 401, "Unauthorized");
        if (stored.RevokedAt.HasValue || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new AppException("Refresh token has expired or was revoked.", 401, "Unauthorized");

        var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == stored.UserId, ct)
                   ?? throw new AppException("Unknown account.", 401, "Unauthorized");
        if (user.Status != UserStatus.Active)
            throw new AppException("Account is not active.", 403, "Forbidden");
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow)
            throw new AppException("Account is temporarily locked.", 423, "Locked");
        var tenantId = stored.TenantId;
        if (!tenantId.HasValue)
        {
            var membership = await db.Set<TenantMembership>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.Status == MembershipStatus.Active, ct);
            tenantId = membership?.TenantId;
        }
        if (!tenantId.HasValue)
            throw new AppException("No active center membership for this token.", 403, "Forbidden");
        var roles = await db.Set<UserRole>().IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id && r.TenantId == tenantId.Value &&
                        (r.ExpiresAt == null || r.ExpiresAt > DateTimeOffset.UtcNow))
            .Select(r => r.Role.Name).ToListAsync(ct);

        var result = await issuer.IssueAsync(user, tenantId.Value, roles, ct);
        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenHash = TokenHasher.Sha256(result.RefreshToken);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("Refresh token was already used.", 401, "Unauthorized");
        }
        return result;
    }
}
