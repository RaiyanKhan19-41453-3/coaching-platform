using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Reporting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Crm;

public sealed record ListGuardianChildren() : IRequest<IReadOnlyList<ChildDto>>, IRequiresTenant;

public sealed record ChildDto(Guid StudentId, string FirstName, string LastName, string Relationship);

public sealed class ListGuardianChildrenHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<ListGuardianChildren, IReadOnlyList<ChildDto>>
{
    public async Task<IReadOnlyList<ChildDto>> Handle(ListGuardianChildren request, CancellationToken ct)
    {
        if (current.UserId is null) throw new ForbiddenException("No authenticated user.");
        return await (from link in db.Set<StudentGuardian>()
                      join user in db.Set<User>() on link.StudentId equals user.Id
                      where link.GuardianId == current.UserId.Value
                      select new ChildDto(user.Id, user.FirstName, user.LastName, link.Relationship.ToString()))
            .ToListAsync(ct);
    }
}

public sealed record GetChildSummary(Guid StudentId) : IRequest<ChildSummaryDto>, IRequiresTenant;

public sealed record ChildSummaryDto(Guid StudentId, string Name, decimal AttendancePercent,
    decimal Collected, decimal Outstanding, decimal AverageGrade, int ActiveEnrollments);

public sealed class GetChildSummaryHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<GetChildSummary, ChildSummaryDto>
{
    public async Task<ChildSummaryDto> Handle(GetChildSummary request, CancellationToken ct)
    {
        var guardian = await db.Set<StudentGuardian>().FirstOrDefaultAsync(g =>
                g.GuardianId == current.UserId && g.StudentId == request.StudentId, ct)
            ?? throw new ForbiddenException("No guardian access to this student.");
        var student = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == request.StudentId, ct)
                      ?? throw new NotFoundException(nameof(User), request.StudentId);
        var attendance = await db.Set<AttendanceStat>().Where(a => a.StudentId == request.StudentId)
            .ToListAsync(ct);
        var fees = await db.Set<FeeSummary>().Where(f => f.StudentId == request.StudentId).ToListAsync(ct);
        var grades = await db.Set<GradeSummary>().Where(g => g.StudentId == request.StudentId).ToListAsync(ct);
        var enrollments = await db.Set<Domain.Academics.Enrollment>().CountAsync(e =>
            e.StudentId == request.StudentId &&
            (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Applied), ct);
        return new ChildSummaryDto(
            student.Id, student.FullName,
            attendance.Count == 0 ? 0m : Math.Round(attendance.Average(a => a.AttendancePercent), 2),
            guardian.CanViewFees ? fees.Sum(f => f.Paid.Amount) : 0m,
            guardian.CanViewFees ? fees.Sum(f => f.Outstanding.Amount) : 0m,
            !guardian.CanViewGrades || grades.Count == 0 ? 0m : Math.Round(grades.Average(g => g.Average), 2),
            enrollments);
    }
}
