using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoachingPlatform.Infrastructure.Persistence;

/// <summary>Design-time factory so `dotnet ef migrations` needs no running API or database.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=coaching;Username=coaching;Password=coaching")
            .Options;
        return new AppDbContext(options, new StaticTenantContext());
    }
}
