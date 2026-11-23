namespace CoachingPlatform.Domain.Common;

public abstract class Entity : IEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj)
        => obj is Entity other && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Base for globally-scoped rows (e.g. User, Tenant, Permission) — no tenant filter.
/// </summary>
public abstract class GlobalEntity : Entity, IAuditableEntity, ISoftDeletable
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

/// <summary>
/// Base for tenant-scoped rows. Combines tenancy, auditing, soft delete and concurrency.
/// </summary>
public abstract class TenantEntity : Entity, ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrent
{
    public Guid TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
