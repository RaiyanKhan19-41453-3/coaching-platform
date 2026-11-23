using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoachingPlatform.Api;

public sealed class RedisHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var value = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(value))
            return HealthCheckResult.Healthy("Redis not configured; SignalR backplane disabled by design.");

        var parts = value.Split(':');
        var host = parts[0];
        var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 6379;
        try
        {
            using var client = new TcpClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, timeout.Token);
            return HealthCheckResult.Healthy("Redis reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Redis unreachable: {ex.Message}");
        }
    }
}
