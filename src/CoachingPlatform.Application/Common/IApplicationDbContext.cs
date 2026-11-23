using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Common;

/// <summary>
/// Application-facing persistence abstraction. DbContext already provides both members,
/// so Infrastructure simply declares the interface without adapter code.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
