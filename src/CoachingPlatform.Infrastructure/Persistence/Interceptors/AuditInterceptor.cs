using System.Text.Json;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoachingPlatform.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Fills audit/tenant fields and emits an append-only audit row for every mutation.
/// Keeps domain entities free of infrastructure concerns.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> SkipAudit = new(StringComparer.Ordinal)
    {
        nameof(AuditLog), nameof(OutboxMessage)
    };

    private readonly ITenantContext _context;

    public AuditInterceptor(ITenantContext context) => _context = context;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is not AppDbContext db) return;
        var now = db.Now;

        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached) continue;
            if (entry.Metadata.IsOwned()) continue; // owned value objects (Money) are not aggregates

            if (entry.Entity is ITenantEntity te && entry.State == EntityState.Added && te.TenantId == Guid.Empty)
                te.TenantId = _context.TenantId;

            if (entry.Entity is IConcurrent concurrent)
            {
                // Application-managed optimistic concurrency token. EF puts the original value
                // in the WHERE clause and the fresh value in SET.
                concurrent.RowVersion = Guid.NewGuid().ToByteArray();
            }

            if (entry.Entity is IAuditableEntity audit)
            {
                if (entry.State == EntityState.Added)
                {
                    audit.CreatedAt = now;
                    audit.CreatedBy = _context.UserId;
                }
                audit.UpdatedAt = now;
                audit.UpdatedBy = _context.UserId;

                if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable soft)
                {
                    soft.DeletedAt = now;
                    soft.DeletedBy = _context.UserId;
                }
            }
        }

        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            if (entry.Metadata.IsOwned()) continue;
            if (SkipAudit.Contains(entry.Entity.GetType().Name)) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var changes = BuildChangeSet(entry);

            var tenantId = entry.Entity is ITenantEntity t ? t.TenantId : _context.TenantId;
            db.AuditLogs.Add(new AuditLog
            {
                TenantId = tenantId == Guid.Empty ? null : tenantId,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Property("Id").CurrentValue?.ToString() ?? "unknown",
                Action = entry.State.ToString(),
                ActorId = _context.UserId,
                ActorEmail = _context.UserEmail,
                ChangesJson = changes,
                IpAddress = _context.IpAddress,
                UserAgent = _context.UserAgent,
                OccurredAt = now
            });
        }
    }

    private static string? BuildChangeSet(EntityEntry entry)
    {
        var parts = new Dictionary<string, object?>();
        foreach (var p in entry.Properties)
        {
            if (entry.State == EntityState.Added || p.IsModified)
                parts[p.Metadata.Name] = new
                {
                    before = entry.State == EntityState.Added ? null : p.OriginalValue,
                    after = p.CurrentValue
                };
        }
        return parts.Count == 0 ? null : JsonSerializer.Serialize(parts);
    }
}
