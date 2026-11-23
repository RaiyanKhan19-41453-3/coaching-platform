using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Domain.Tenancy;

/// <summary>
/// A coaching center (or a whole franchise group). The root of every tenant-scoped aggregate.
/// Deliberately NOT ITenantEntity — it is the tenant itself.
/// </summary>
public sealed class Tenant : GlobalEntity, IConcurrent
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;          // URL-safe unique identifier
    public string? Subdomain { get; set; }                // e.g. "army-academy"
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Dhaka";
    public string DefaultCurrency { get; set; } = "BDT";
    public string DefaultLanguage { get; set; } = "en";
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public TenantPlan Plan { get; set; } = TenantPlan.Free;
    public DateTimeOffset? TrialEndsAt { get; set; }
    public string? SettingsJson { get; set; }             // branding, fee rules, notification defaults
    public byte[] RowVersion { get; set; } = [];

    public ICollection<TenantSetting> Settings { get; set; } = [];
}

public sealed class TenantSetting : TenantEntity
{
    public string Category { get; set; } = "General";
    public string Key { get; set; } = default!;
    public string? Value { get; set; }
    public bool IsPublic { get; set; }
}

/// <summary>Per-tenant feature toggles; supports gradual rollout across centers.</summary>
public sealed class FeatureFlag : TenantEntity
{
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public string? TargetingJson { get; set; }  // rollout % / role targeting
}
