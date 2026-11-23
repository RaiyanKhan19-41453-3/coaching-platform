using System.Security.Claims;
using CoachingPlatform.Infrastructure.Persistence;

namespace CoachingPlatform.Api;

/// <summary>
/// Resolves the current tenant from (in priority order) a verified JWT claim, an explicit
/// X-Tenant-Id header (dev/ops), or the request subdomain. Scoped to one HTTP request.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpTenantContext(IHttpContextAccessor accessor) => _accessor = accessor;

    private HttpContext? Http => _accessor.HttpContext;

    public Guid TenantId
    {
        get
        {
            var http = Http;
            if (http is null) return Guid.Empty;

            var claim = http.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var fromClaim) && fromClaim != Guid.Empty) return fromClaim;

            if (Guid.TryParse(http.Request.Headers["X-Tenant-Id"], out var fromHeader) && fromHeader != Guid.Empty)
                return fromHeader;

            return Guid.TryParse(http.Items["resolved_tenant_id"]?.ToString(), out var fromMiddleware)
                ? fromMiddleware
                : Guid.Empty;
        }
    }

    public Guid? UserId
    {
        get
        {
            var value = Http?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? Http?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserEmail => Http?.User.FindFirst(ClaimTypes.Email)?.Value
                                ?? Http?.User.FindFirst("email")?.Value;

    public string? IpAddress => Http?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => Http?.Request.Headers.UserAgent.ToString();

    public bool IsSystem => false;
}
