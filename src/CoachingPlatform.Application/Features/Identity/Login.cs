using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Tenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Identity;

public sealed record Login(string Email, string Password, string? TenantSlug) : IRequest<AuthTokensDto>;

public sealed class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(
    IApplicationDbContext db, IPasswordHasher hasher, TokenIssuer issuer) : IRequestHandler<Login, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(Login request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || user.PasswordHash is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new AppException("Invalid email or password.", 401, "Unauthorized");
        if (user.Status != UserStatus.Active)
            throw new AppException("Account is not active.", 403, "Forbidden");
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow)
            throw new AppException("Account is temporarily locked.", 423, "Locked");

        var memberships = await db.Set<TenantMembership>().IgnoreQueryFilters()
            .Where(m => m.UserId == user.Id && m.Status == MembershipStatus.Active)
            .Select(m => m.TenantId).ToListAsync(ct);
        if (memberships.Count == 0)
            throw new AppException("No active center membership.", 403, "Forbidden");

        Guid tenantId;
        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            var slug = request.TenantSlug.Trim().ToLowerInvariant();
            var tenantIdFromSlug = await db.Set<Tenant>().IgnoreQueryFilters()
                .Where(t => t.Slug == slug || t.Subdomain == slug)
                .Select(t => (Guid?)t.Id).FirstOrDefaultAsync(ct);
            if (!tenantIdFromSlug.HasValue || !memberships.Contains(tenantIdFromSlug.Value))
                throw new AppException("Tenant not found for this account.", 403, "Forbidden");
            tenantId = tenantIdFromSlug.Value;
        }
        else if (memberships.Count == 1) tenantId = memberships[0];
        else throw new ConflictException("Multiple centers: supply tenantSlug.");

        var roles = await db.Set<UserRole>().IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id && r.TenantId == tenantId && (r.ExpiresAt == null || r.ExpiresAt > DateTimeOffset.UtcNow))
            .Select(r => r.Role.Name).ToListAsync(ct);

        return await issuer.IssueAsync(user, tenantId, roles, ct);
    }

}
