using CoachingPlatform.Api.Jobs;
using CoachingPlatform.Application.Services;

namespace CoachingPlatform.Api;

/// <summary>API-side implementation of the operational jobs abstraction.</summary>
public sealed class JobService(OutboxDispatcher dispatcher, ProjectionRebuilder rebuilder) : IJobService
{
    public Task DispatchOutboxAsync(CancellationToken ct) => dispatcher.DispatchCoreAsync(ct);
    public Task RebuildProjectionsAsync(CancellationToken ct) => rebuilder.RebuildAsync();
}
