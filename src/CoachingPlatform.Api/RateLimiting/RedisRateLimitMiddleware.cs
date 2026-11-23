using StackExchange.Redis;

namespace CoachingPlatform.Api.RateLimiting;

/// <summary>Global per-tenant quota shared across replicas. Fails open if Redis is down.</summary>
public sealed class RedisRateLimitMiddleware(
    RequestDelegate next, IConnectionMultiplexer? redis, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (redis is not null)
        {
            var tenant = context.User?.FindFirst("tenant_id")?.Value;
            var key = string.IsNullOrWhiteSpace(tenant)
                ? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                : $"tenant:{tenant}";
            var limit = int.TryParse(configuration["RateLimit:PerTenantPerMinute"], out var configured)
                ? configured : 6000;
            var window = $"{DateTimeOffset.UtcNow:yyyyMMddHHmm}";
            try
            {
                var database = redis.GetDatabase();
                var count = await database.StringIncrementAsync($"ratelimit:{key}:{window}");
                if (count == 1) await database.KeyExpireAsync($"ratelimit:{key}:{window}", TimeSpan.FromMinutes(2));
                if (count > limit)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return;
                }
            }
            catch (Exception)
            {
                // Redis unavailable: in-process limiter still applies.
            }
        }
        await next(context);
    }
}
