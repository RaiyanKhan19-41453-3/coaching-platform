using System.Text.Json;
using CoachingPlatform.Infrastructure.Persistence;

namespace CoachingPlatform.Api.Jobs;

public interface IOutboxEventHandler
{
    string EventType { get; }
    Task HandleAsync(JsonElement payload, AppDbContext db, Guid tenantId, CancellationToken ct);
}
