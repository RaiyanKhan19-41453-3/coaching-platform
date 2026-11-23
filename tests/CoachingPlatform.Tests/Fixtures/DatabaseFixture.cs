using Xunit;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CoachingPlatform.Tests.Fixtures;

/// <summary>One real Postgres container shared by the integration collection.</summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("coaching_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var ambient = new StaticTenantContext();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options;
        await using var db = new AppDbContext(options, ambient);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}

[CollectionDefinition("database", DisableParallelization = true)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>;

/// <summary>Mutable ambient context shared by a test method. Query filters read TenantId from it.</summary>
public sealed class TestAmbientContext : ITenantContext, ICurrentUser
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public string? IpAddress => "127.0.0.1";
    public string? UserAgent => "test-runner";
    public bool IsSystem => true;
}

public sealed class FakeTokenGenerator : ITokenGenerator
{
    public GeneratedToken Generate(Guid userId, Guid tenantId, string email, IReadOnlyList<string> roles)
        => new($"access-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddMinutes(30), Guid.NewGuid().ToString("N"));
}
