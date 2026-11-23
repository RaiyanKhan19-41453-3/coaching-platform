namespace CoachingPlatform.Application.Services;

/// <summary>Operational abstraction so controllers never reference infrastructure jobs.</summary>
public interface IJobService
{
    Task DispatchOutboxAsync(CancellationToken ct = default);
    Task RebuildProjectionsAsync(CancellationToken ct = default);
}
