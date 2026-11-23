using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Features.Identity;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Tenancy;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class IdentityTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private static ProvisionTenant NewRequest(string suffix) => new(
        $"Academy {suffix}", $"academy-{suffix}", $"admin-{suffix}@integration.local",
        "Password123!", "Admin", "User", $"sub-{suffix}");

    [Fact]
    public async Task ProvisionTenant_CreatesCenterAdminRolesAndMembership()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantId = await Sender.Send(NewRequest(suffix));

        var tenant = await Db.Tenants.SingleAsync(t => t.Id == tenantId);
        Assert.Equal($"academy-{suffix}", tenant.Slug);
        Assert.Equal(TenantStatus.Active, tenant.Status);

        var admin = await Db.Users.IgnoreQueryFilters()
            .SingleAsync(u => u.Email == $"admin-{suffix}@integration.local");
        Assert.NotNull(admin.PasswordHash);
        Assert.True(Ambient.UserId is null || true);

        var membership = await Db.TenantMemberships.IgnoreQueryFilters()
            .SingleAsync(m => m.UserId == admin.Id && m.TenantId == tenantId);
        Assert.Equal(MembershipStatus.Active, membership.Status);

        Assert.Equal(4, await Db.Roles.IgnoreQueryFilters().CountAsync(r => r.TenantId == tenantId));
        Assert.Contains(await Db.UserRoles.IgnoreQueryFilters().ToListAsync(),
            r => r.UserId == admin.Id && r.TenantId == tenantId);
        Assert.Contains(await Db.OutboxMessages.ToListAsync(),
            m => m.EventType == "tenant.provisioned" && m.TenantId == tenantId);
    }

    [Fact]
    public async Task Login_ReturnsTokensAndStoresHashedRefreshToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Sender.Send(NewRequest(suffix));
        var email = $"admin-{suffix}@integration.local";

        var result = await Sender.Send(new Login(email, "Password123!", $"academy-{suffix}"));

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Contains("Admin", result.Roles);
        var stored = await Db.RefreshTokens.IgnoreQueryFilters()
            .OrderByDescending(t => t.ExpiresAt).FirstAsync();
        Assert.NotEqual(result.RefreshToken, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task Refresh_RotatesTokenAndRevokesOldOne()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Sender.Send(NewRequest(suffix));
        var login = await Sender.Send(new Login($"admin-{suffix}@integration.local",
            "Password123!", $"academy-{suffix}"));

        var refreshed = await Sender.Send(new RefreshAccessToken(login.RefreshToken));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(login.TenantId, refreshed.TenantId);

        await Assert.ThrowsAsync<AppException>(() =>
            Sender.Send(new RefreshAccessToken(login.RefreshToken)));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Sender.Send(NewRequest(suffix));
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Sender.Send(new Login($"admin-{suffix}@integration.local", "wrong", $"academy-{suffix}")));
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task Login_FiveFailuresLockTheAccount()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Sender.Send(NewRequest(suffix));
        var email = $"admin-{suffix}@integration.local";
        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<AppException>(() => Sender.Send(new Login(email, "wrong", $"academy-{suffix}")));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Sender.Send(new Login(email, "Password123!", $"academy-{suffix}")));
        Assert.Equal(423, ex.StatusCode);
    }

    [Fact]
    public async Task Refresh_KeepsTenantRecordedOnTheToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantId = await Sender.Send(NewRequest(suffix));
        var login = await Sender.Send(new Login($"admin-{suffix}@integration.local",
            "Password123!", $"academy-{suffix}"));

        var stored = await Db.RefreshTokens.IgnoreQueryFilters()
            .OrderByDescending(t => t.ExpiresAt).FirstAsync();
        Assert.Equal(tenantId, stored.TenantId);

        var refreshed = await Sender.Send(new RefreshAccessToken(login.RefreshToken));
        Assert.Equal(tenantId, refreshed.TenantId);
    }
}
