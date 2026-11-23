using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Domain.Scheduling;
using CoachingPlatform.Domain.Academics;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Scheduling;

public sealed record AttendanceMark(Guid StudentId, AttendanceStatus Status, int MinutesLate = 0, string? Note = null);
public sealed record MarkAttendance(Guid SessionId, IReadOnlyList<AttendanceMark> Records)
    : IRequest<int>, IRequiresTenant;

public sealed class MarkAttendanceValidator : AbstractValidator<MarkAttendance>
{
    public MarkAttendanceValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Records).NotEmpty().Must(r => r.Count <= 500).WithMessage("At most 500 students at once.");
        RuleForEach(x => x.Records).ChildRules(r =>
        {
            r.RuleFor(x => x.StudentId).NotEmpty();
            r.RuleFor(x => x.MinutesLate).InclusiveBetween(0, 600);
        });
    }
}

public sealed class MarkAttendanceHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<MarkAttendance, int>
{
    public async Task<int> Handle(MarkAttendance request, CancellationToken ct)
    {
        var session = await db.Set<ClassSession>().FirstOrDefaultAsync(s => s.Id == request.SessionId, ct)
                      ?? throw new NotFoundException(nameof(ClassSession), request.SessionId);

        var studentIds = request.Records.Select(r => r.StudentId).Distinct().ToList();
        var enrolledIds = await db.Set<Domain.Academics.Enrollment>()
            .Where(e => e.BatchId == session.BatchId && studentIds.Contains(e.StudentId) &&
                        e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Transferred)
            .Select(e => e.StudentId).ToListAsync(ct);
        var unknown = studentIds.Except(enrolledIds).ToList();
        if (unknown.Count > 0)
            throw new ConflictException($"Students {string.Join(", ", unknown)} are not enrolled in this batch.");
        var existing = await db.Set<Attendance>()
            .Where(a => a.SessionId == session.Id && studentIds.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId, ct);

        var absentStudents = new List<Guid>();
        var written = 0;
        foreach (var mark in request.Records)
        {
            if (existing.TryGetValue(mark.StudentId, out var row))
            {
                row.Status = mark.Status;
                row.MinutesLate = mark.MinutesLate;
                row.Note = mark.Note;
                row.Method = AttendanceMethod.Manual;
                row.CheckedInAt = mark.Status is AttendanceStatus.Present or AttendanceStatus.Late
                    ? DateTimeOffset.UtcNow : null;
                row.MarkedBy = current.UserId;
            }
            else
            {
                db.Set<Attendance>().Add(new Attendance
                {
                    TenantId = current.TenantId,
                    SessionId = session.Id,
                    StudentId = mark.StudentId,
                    Status = mark.Status,
                    MinutesLate = mark.MinutesLate,
                    Note = mark.Note,
                    Method = AttendanceMethod.Manual,
                    CheckedInAt = mark.Status is AttendanceStatus.Present or AttendanceStatus.Late
                        ? DateTimeOffset.UtcNow : null,
                    MarkedBy = current.UserId
                });
            }
            if (mark.Status == AttendanceStatus.Absent) absentStudents.Add(mark.StudentId);
            written++;
        }

        session.Status = SessionStatus.Completed;
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId,
            EventType = EventTypes.AttendanceMarked,
            PayloadJson = JsonSerializer.Serialize(new
            {
                session.Id, session.BatchId, session.StartUtc,
                StudentIds = studentIds, AbsentStudents = absentStudents
            }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return written;
    }
}
