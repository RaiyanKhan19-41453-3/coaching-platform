namespace CoachingPlatform.Domain.Common;

/// <summary>Marker for all persisted aggregates.</summary>
public interface IEntity
{
    Guid Id { get; }
}

/// <summary>Scopes a row to a tenant. Enforced with a global EF query filter.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

/// <summary>Soft delete. Rows are never hard-deleted from the API surface.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

/// <summary>Audit fields populated automatically by a SaveChanges interceptor.</summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

/// <summary>Optimistic concurrency token for write-heavy aggregates.</summary>
public interface IConcurrent
{
    byte[] RowVersion { get; set; }
}
