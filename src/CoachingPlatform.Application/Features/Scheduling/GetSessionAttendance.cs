using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Scheduling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Scheduling;

public sealed record GetSessionAttendance(Guid SessionId) : IRequest<IReadOnlyList<AttendanceDto>>, IRequiresTenant;

public sealed class GetSessionAttendanceHandler(IApplicationDbContext db)
    : IRequestHandler<GetSessionAttendance, IReadOnlyList<AttendanceDto>>
{
    public async Task<IReadOnlyList<AttendanceDto>> Handle(GetSessionAttendance request, CancellationToken ct)
        => await db.Set<Attendance>().AsNoTracking()
            .Where(a => a.SessionId == request.SessionId)
            .Select(a => new AttendanceDto(a.StudentId, a.Status, a.Method, a.CheckedInAt, a.MinutesLate, a.Note))
            .ToListAsync(ct);
}

public sealed record AttendanceDto(
    Guid StudentId, AttendanceStatus Status, AttendanceMethod Method,
    DateTimeOffset? CheckedInAt, int MinutesLate, string? Note);
