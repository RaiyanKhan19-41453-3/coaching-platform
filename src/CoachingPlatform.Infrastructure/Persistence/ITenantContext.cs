namespace CoachingPlatform.Infrastructure.Persistence;

/// <summary>
/// Ambient request context consumed by query filters and the audit interceptor.
/// The API implements this from the current HttpContext; background jobs / design-time
/// use <see cref="StaticTenantContext"/>.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsSystem { get; }
}

/// <summary>Used by migrations, seeding and outbox workers where there is no HttpContext.</summary>
public sealed class StaticTenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSystem { get; set; } = true;
}
