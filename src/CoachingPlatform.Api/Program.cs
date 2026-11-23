using CoachingPlatform.Api;
using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=db;Database=coaching;Username=coaching;Password=coaching;Include Error Detail=true";

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null));
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);
builder.Services.AddSignalR();

builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
          .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseCors("web");

// Resolve subdomain tenants before any DbContext is used in the pipeline.
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    if (host.Contains('.') && !host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
    {
        var subdomain = host.Split('.')[0];
        context.Items["resolved_tenant_slug"] = subdomain;
    }
    await next();
});

app.MapGet("/", () => new
{
    service = "Coaching Platform API",
    status = "running",
    stack = new[] { ".NET 10", "EF Core 10", "PostgreSQL 17", "Redis 7" }
});

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHub<NotificationHub>("/hubs/notifications");

// Bootstrap schema + seed data with retry (Postgres may still be starting).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await DatabaseSeeder.SeedAsync(connectionString);
            app.Logger.LogInformation("Database ready (attempt {Attempt}).", attempt);
            break;
        }
        catch (PostgresException ex) when (attempt < 15)
        {
            app.Logger.LogWarning(ex, "Database not ready (attempt {Attempt}); retrying.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

app.Run();
