using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api.Jobs;

/// <summary>
/// Background jobs have no HttpContext. Each context is created with an explicit system
/// tenant so query filters resolve correctly for the event being processed.
/// </summary>
public sealed class JobDbContextFactory(string connectionString)
{
    private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(connectionString, o => o.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null))
        .Options;

    public AppDbContext Create(Guid tenantId) => CreateFor(new StaticTenantContext
    {
        TenantId = tenantId, IsSystem = true, UserEmail = "system@coaching-platform"
    });

    public AppDbContext CreateFor(ITenantContext context) => new(
        new DbContextOptionsBuilder<AppDbContext>(_options)
            .AddInterceptors(new AuditInterceptor(context))
            .Options,
        context,
        TimeProvider.System);
}
