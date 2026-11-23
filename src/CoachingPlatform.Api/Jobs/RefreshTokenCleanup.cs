using CoachingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs;

public sealed class RefreshTokenCleanup(JobDbContextFactory factory)
{
    public async Task CleanupAsync()
    {
        await using var db = factory.Create(Guid.Empty);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var removed = await db.RefreshTokens.IgnoreQueryFilters()
            .Where(t => t.ExpiresAt < cutoff || (t.RevokedAt != null && t.RevokedAt < cutoff))
            .ExecuteDeleteAsync();
        if (removed > 0)
            Console.WriteLine($"[housekeeping] removed {removed} expired refresh tokens");
    }
}
