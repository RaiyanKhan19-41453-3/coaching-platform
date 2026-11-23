using CoachingPlatform.Application.Common;
using MediatR;

namespace CoachingPlatform.Application.Behaviors;

public sealed class TenantGuardBehavior<TRequest, TResponse>(ICurrentUser current)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequiresTenant && current.TenantId == Guid.Empty)
            throw new ForbiddenException("A tenant context is required for this operation.");
        return await next();
    }
}
