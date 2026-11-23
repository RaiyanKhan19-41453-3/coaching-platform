using CoachingPlatform.Application.DTOs.Identity;
using CoachingPlatform.Application.Services;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

/// <summary>Controllers depend on these interfaces, so this is the contract they exercise.</summary>
public class ServiceLayerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task AuthService_ProvisionThenLogin_ReturnsTokens()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var auth = Services.GetRequiredService<IAuthService>();

        var provisioned = await auth.ProvisionTenantAsync(new ProvisionTenantRequest(
            $"Layer {suffix}", $"layer-{suffix}", $"layer-{suffix}@test.local",
            "Password123!", "Layer", "User", null));
        Assert.NotEqual(Guid.Empty, provisioned.TenantId);

        var tokens = await auth.LoginAsync(new LoginRequest(
            $"layer-{suffix}@test.local", "Password123!", $"layer-{suffix}"));
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.Equal(provisioned.TenantId, tokens.TenantId);
        Assert.Contains("Admin", tokens.Roles);
    }
}
