using Xunit;
using CoachingPlatform.Application;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Features.Identity;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Tenancy;
using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoachingPlatform.Tests.Fixtures;

[Collection("database")]
public abstract class IntegrationTestBase(DatabaseFixture fixture) : IAsyncLifetime
{
    private ServiceProvider _services = null!;
    protected TestAmbientContext Ambient { get; } = new();

    protected ISender Sender => _services.GetRequiredService<ISender>();
    protected AppDbContext Db => _services.GetRequiredService<AppDbContext>();

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ITenantContext>(Ambient);
        services.AddSingleton<ICurrentUser>(Ambient);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, FakeTokenGenerator>();
        services.AddSingleton(_ => new AuditInterceptor(Ambient));
        services.AddScoped(provider => new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(fixture.ConnectionString)
                .AddInterceptors(provider.GetRequiredService<AuditInterceptor>())
                .Options,
            Ambient));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        _services = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }

    protected async Task<Guid> NewTenantAsync(string slug = "test-center")
    {
        slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
        var tenant = new Tenant
        { Name = "Test Center", Slug = slug,
            Subdomain = slug, Status = TenantStatus.Active, Plan = TenantPlan.Growth
        };
        Db.Tenants.Add(tenant);
        await Db.SaveChangesAsync();
        Ambient.TenantId = tenant.Id;
        Ambient.UserId = Guid.NewGuid();
        Ambient.UserEmail = "admin@integration.local";
        Ambient.Roles = ["Admin"];
        return tenant.Id;
    }

    protected async Task<Guid> NewUserAsync(string? email = null)
    {
        var user = new User
        {
            Email = email ?? $"student-{Guid.NewGuid().ToString("N")[..8]}@test.local",
            EmailConfirmed = true, PhoneNumberConfirmed = false,
            FirstName = "Test", LastName = "User", Status = UserStatus.Active
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user.Id;
    }

    protected async Task<(Guid CourseId, Guid YearId)> NewCourseAndYearAsync()
    {
        var year = new AcademicYear
        { Name = $"AY-{Guid.NewGuid():N}",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2027, 12, 31), IsActive = true
        };
        var course = new Course
        { Code = $"C-{Guid.NewGuid():N}"[..10],
            Name = "Test Course", DurationMonths = 6, IsActive = true
        };
        Db.AcademicYears.Add(year);
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return (course.Id, year.Id);
    }
}
