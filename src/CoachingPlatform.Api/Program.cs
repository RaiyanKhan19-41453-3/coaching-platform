using CoachingPlatform.Api;
using CoachingPlatform.Application;
using CoachingPlatform.Application.Common;
using System.Text;
using CoachingPlatform.Api.Jobs;
using CoachingPlatform.Api.Jobs.Handlers;
using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Storage;
using Hangfire;
using Hangfire.Common;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using CoachingPlatform.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=db;Database=coaching;Username=coaching;Password=coaching;Include Error Detail=true";

builder.Services.AddApplication();
builder.Services.AddSingleton<IObjectStorage>(new MinioObjectStorage(
    builder.Configuration["Minio:Endpoint"] ?? "minio:9000",
    builder.Configuration["Minio:AccessKey"] ?? "coaching",
    builder.Configuration["Minio:SecretKey"] ?? "coaching",
    builder.Configuration["Minio:Bucket"] ?? "coaching",
    builder.Configuration["Minio:PublicEndpoint"] ?? "http://localhost:9000"));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-signing-key-change-this-in-production-1234";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "coaching-platform",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "coaching-web",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// ---- Durable background jobs ----
builder.Services.AddSingleton(new JobDbContextFactory(connectionString));
builder.Services.AddTransient<OutboxDispatcher>();
builder.Services.AddTransient<RefreshTokenCleanup>();
builder.Services.AddTransient<ProjectionRebuilder>();
builder.Services.AddScoped<IOutboxEventHandler, EnrollmentCreatedHandler>();
builder.Services.AddScoped<IOutboxEventHandler, AttendanceMarkedHandler>();
builder.Services.AddScoped<IOutboxEventHandler, AssessmentSubmittedHandler>();
builder.Services.AddScoped<IOutboxEventHandler, PaymentReceivedHandler>();
builder.Services.AddScoped<IOutboxEventHandler, InvoiceCreatedHandler>();
builder.Services.AddScoped<IOutboxEventHandler, TenantProvisionedHandler>();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HttpTenantContext>());
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpTenantContext>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null));
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);
var signalR = builder.Services.AddSignalR();
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
    signalR.AddStackExchangeRedis(redisConnection, o => o.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("coaching:"));

builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
          .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("web");
if (app.Environment.IsDevelopment()) app.UseHangfireDashboard("/jobs", new DashboardOptions { Authorization = [] });

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

app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();
app.MapAuth();
app.MapAssets();
app.MapApi();

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
            try
            {
                await app.Services.GetRequiredService<IObjectStorage>().EnsureBucketAsync();
            }
            catch (Exception storageEx)
            {
                app.Logger.LogWarning(storageEx, "Object storage bucket not ready; asset endpoints will fail until it is.");
            }
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

RecurringJob.AddOrUpdate<OutboxDispatcher>("outbox-dispatch",
    job => job.DispatchAsync(), "*/1 * * * *");
RecurringJob.AddOrUpdate<RefreshTokenCleanup>("refresh-token-cleanup",
    job => job.CleanupAsync(), Cron.Daily);
RecurringJob.AddOrUpdate<ProjectionRebuilder>("projection-rebuild",
    job => job.RebuildAsync(), Cron.Daily);

app.Run();
