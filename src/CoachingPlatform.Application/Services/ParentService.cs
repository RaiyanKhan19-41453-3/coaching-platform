using CoachingPlatform.Application.DTOs.Crm;
using CoachingPlatform.Application.Features.Crm;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IParentService
{
    Task<IReadOnlyList<ChildResponse>> ListChildrenAsync(CancellationToken ct = default);
    Task<ChildSummaryResponse> GetChildSummaryAsync(Guid studentId, CancellationToken ct = default);
}

public sealed class ParentService(ISender sender) : IParentService
{
    public async Task<IReadOnlyList<ChildResponse>> ListChildrenAsync(CancellationToken ct)
    {
        var rows = await sender.Send(new ListGuardianChildren(), ct);
        return rows.Select(r => new ChildResponse(r.StudentId, r.FirstName, r.LastName, r.Relationship)).ToList();
    }

    public async Task<ChildSummaryResponse> GetChildSummaryAsync(Guid studentId, CancellationToken ct)
    {
        var r = await sender.Send(new GetChildSummary(studentId), ct);
        return new ChildSummaryResponse(r.StudentId, r.Name, r.AttendancePercent,
            r.Collected, r.Outstanding, r.AverageGrade, r.ActiveEnrollments);
    }
}
