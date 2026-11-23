using CoachingPlatform.Application.Features.Identity;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class AuditTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task AuditLog_RedactsPasswordAndTokenSecrets()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Sender.Send(new ProvisionTenant(
            $"Audit {suffix}", $"audit-{suffix}", $"audit-{suffix}@test.local",
            "Password123!", "Audit", "User"));

        var logs = await Db.AuditLogs.Where(a => a.EntityName == nameof(Domain.Identity.User)).ToListAsync();
        Assert.NotEmpty(logs);
        Assert.DoesNotContain(logs, l => l.ChangesJson != null &&
            (l.ChangesJson.Contains("PasswordHash") || l.ChangesJson.Contains("MfaSecret")));
    }
}
