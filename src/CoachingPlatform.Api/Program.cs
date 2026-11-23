using System.Text;
using System.Threading.RateLimiting;
using CoachingPlatform.Api;
using CoachingPlatform.Api.Jobs;
using CoachingPlatform.Api.Jobs.Handlers;
using CoachingPlatform.Application;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Services;
using CoachingPlatform.Infrastructure.Persistence;
using CoachingPlatform.Infrastructure.Persistence.Interceptors;
using CoachingPlatform.Infrastructure.Storage;
using Hangfire;
using Hangfire.Common;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
                 .ReadFrom.Services(services)
                 .Enrich.FromLogContext()
                 .WriteTo.Console();
    var seq = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seq)) configuration.WriteTo.Seq(seq);
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=db;Database=coaching;Username=coaching;Password=coaching;Include Error Detail=true";
var redisConnection = builder.Configuration["Redis:ConnectionString"];
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-signing-key-change-this-in-production-1234";

if (builder.Environment.IsProduction())
{
    if (jwtKey.Length < 32 || jwtKey.Contains("dev-only", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Jwt:Key must be a strong, non-default secret in Production.");
    if (new NpgsqlConnectionStringBuilder(connectionString).Password == "coaching")
        throw new InvalidOperationException("The default database password is not allowed in Production.");
    if (builder.Configuration["Minio:SecretKey"] == "coaching")
        throw new InvalidOperationException("The default MinIO secret is not allowed in Production.");
}

// ---- Application services ----
builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddSingleton<IObjectStorage>(new MinioObjectStorage(
    builder.Configuration["Minio:Endpoint"] ?? "minio:9000",
    builder.Configuration["Minio:AccessKey"] ?? "coaching",
    builder.Configuration["Minio:SecretKey"] ?? "coaching",
    builder.Configuration["Minio:Bucket"] ?? "coaching",
    builder.Configuration["Minio:PublicEndpoint"] ?? "http://localhost:9000"));
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = int.TryParse(builder.Configuration["RateLimit:AuthPerIpPerMinute"], out var authLimit) ? authLimit : 100,
            Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var tenant = context.User?.FindFirst("tenant_id")?.Value;
        var key = string.IsNullOrWhiteSpace(tenant)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
            : $"tenant:{tenant}";
        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = int.TryParse(builder.Configuration["RateLimit:PerTenantPerMinute"], out var tenantLimit) ? tenantLimit : 6000,
            Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6, QueueLimit = 0
        });
    });
});

// ---- Persistence & observability ----
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

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

var telemetry = builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
var otlp = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(otlp))
{
    telemetry.WithTracing(t => t.AddOtlpExporter(o => o.Endpoint = new Uri(otlp)));
    telemetry.WithMetrics(m => m.AddOtlpExporter(o => o.Endpoint = new Uri(otlp)));
}

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var signalR = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redisConnection))
    signalR.AddStackExchangeRedis(redisConnection,
        o => o.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("coaching:"));

builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:13000"])
          .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---- Background jobs ----
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

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();  // after authentication so the global limit is keyed by tenant
app.UseAuthorization();
app.UseCors("web");
if (app.Environment.IsDevelopment()) app.UseHangfireDashboard("/jobs", new DashboardOptions { Authorization = [] });

// Resolve a subdomain to a tenant id before any handler relies on tenant context.
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    if (host.Contains('.') && !host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
    {
        var slug = host.Split('.')[0];
        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = await db.Tenants.IgnoreQueryFilters()
            .Where(t => t.Slug == slug || t.Subdomain == slug)
            .Select(t => (Guid?)t.Id).FirstOrDefaultAsync();
        if (id.HasValue) context.Items["resolved_tenant_id"] = id.Value;
    }
    await next();
});

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" })).DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).DisableRateLimiting();
app.MapOpenApi().DisableRateLimiting();
app.MapScalarApiReference().DisableRateLimiting();
app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization().DisableRateLimiting();

// Bootstrap. Production expects migrations to have been applied by the migrate job.
using (var scope = app.Services.CreateScope())
{
    var storage = app.Services.GetRequiredService<IObjectStorage>();
    try { await storage.EnsureBucketAsync(); }
    catch (Exception ex) { app.Logger.LogWarning(ex, "Object storage not ready yet."); }

    if (!app.Environment.IsProduction())
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
}

RecurringJob.AddOrUpdate<OutboxDispatcher>("outbox-dispatch", job => job.DispatchAsync(), "*/10 * * * * *");
RecurringJob.AddOrUpdate<RefreshTokenCleanup>("refresh-token-cleanup", job => job.CleanupAsync(), Cron.Daily);
RecurringJob.AddOrUpdate<ProjectionRebuilder>("projection-rebuild", job => job.RebuildAsync(), Cron.Daily);

app.Run();
