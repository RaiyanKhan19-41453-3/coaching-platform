using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Academics;

public sealed record GetBatches(BatchStatus? Status, string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<BatchDto>>, IRequiresTenant;

public sealed class GetBatchesHandler(IApplicationDbContext db)
    : IRequestHandler<GetBatches, PagedResult<BatchDto>>
{
    public async Task<PagedResult<BatchDto>> Handle(GetBatches request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Set<Batch>().AsNoTracking().AsQueryable();

        if (request.Status.HasValue) query = query.Where(b => b.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(b => b.Name.Contains(term) || b.Code.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(b => b.StartDate)
            .Skip((page - 1) * size).Take(size)
            .Select(b => new BatchDto(b.Id, b.Code, b.Name, b.Status, b.StartDate, b.EndDate, b.Capacity))
            .ToListAsync(ct);
        return new PagedResult<BatchDto>(items, total, page, size);
    }
}

public sealed record BatchDto(
    Guid Id, string Code, string Name, BatchStatus Status, DateOnly StartDate, DateOnly EndDate, int Capacity);
