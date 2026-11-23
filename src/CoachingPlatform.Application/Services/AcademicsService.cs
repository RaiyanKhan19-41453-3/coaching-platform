using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.DTOs.Academics;
using CoachingPlatform.Application.Features.Academics;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IAcademicsService
{
    Task<Guid> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default);
    Task<Guid> CreateBatchAsync(CreateBatchRequest request, CancellationToken ct = default);
    Task<PagedResult<BatchResponse>> GetBatchesAsync(BatchQuery query, CancellationToken ct = default);
    Task<Guid> EnrollStudentAsync(Guid batchId, EnrollStudentRequest request, CancellationToken ct = default);
}

public sealed class AcademicsService(ISender sender) : IAcademicsService
{
    public Task<Guid> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct)
        => sender.Send(new CreateCourse(request.Code, request.Name, request.Description,
            request.Level, request.DurationMonths), ct);

    public Task<Guid> CreateBatchAsync(CreateBatchRequest request, CancellationToken ct)
        => sender.Send(new CreateBatch(request.CourseId, request.AcademicYearId, request.TermId,
            request.Name, request.Code, request.StartDate, request.EndDate, request.Capacity), ct);

    public async Task<PagedResult<BatchResponse>> GetBatchesAsync(BatchQuery query, CancellationToken ct)
    {
        var result = await sender.Send(new GetBatches(query.Status, query.Search,
            query.Page == 0 ? 1 : query.Page, query.PageSize == 0 ? 20 : query.PageSize), ct);
        var items = result.Items.Select(b => new BatchResponse(
            b.Id, b.Code, b.Name, b.Status, b.StartDate, b.EndDate, b.Capacity)).ToList();
        return new PagedResult<BatchResponse>(items, result.Total, result.Page, result.PageSize);
    }

    public Task<Guid> EnrollStudentAsync(Guid batchId, EnrollStudentRequest request, CancellationToken ct)
        => sender.Send(new EnrollStudent(batchId, request.StudentId), ct);
}
