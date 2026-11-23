using CoachingPlatform.Application.DTOs.Scheduling;
using CoachingPlatform.Application.Features.Scheduling;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface ISchedulingService
{
    Task<Guid> CreateScheduleRuleAsync(CreateScheduleRuleRequest request, CancellationToken ct = default);
    Task<int> GenerateSessionsAsync(Guid ruleId, GenerateSessionsRequest request, CancellationToken ct = default);
    Task<int> MarkAttendanceAsync(Guid sessionId, MarkAttendanceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceResponse>> GetSessionAttendanceAsync(Guid sessionId, CancellationToken ct = default);
    Task<Guid> CreateLeaveRequestAsync(CreateLeaveRequestRequest request, CancellationToken ct = default);
    Task<bool> ReviewLeaveRequestAsync(Guid id, ReviewLeaveRequestRequest request, CancellationToken ct = default);
}

public sealed class SchedulingService(ISender sender) : ISchedulingService
{
    public Task<Guid> CreateScheduleRuleAsync(CreateScheduleRuleRequest request, CancellationToken ct)
        => sender.Send(new CreateScheduleRule(request.BatchId, request.DayOfWeek, request.StartTime,
            request.EndTime, request.ValidFrom, request.ValidTo, request.TeacherId,
            request.RoomId, request.TopicId, request.RecurrenceRule), ct);

    public Task<int> GenerateSessionsAsync(Guid ruleId, GenerateSessionsRequest request, CancellationToken ct)
        => sender.Send(new GenerateSessions(ruleId, request.From, request.To), ct);

    public async Task<int> MarkAttendanceAsync(Guid sessionId, MarkAttendanceRequest request, CancellationToken ct)
    {
        var marks = request.Records.Select(r => new AttendanceMark(
            r.StudentId, r.Status, r.MinutesLate, r.Note)).ToList();
        return await sender.Send(new MarkAttendance(sessionId, marks), ct);
    }

    public Task<Guid> CreateLeaveRequestAsync(CreateLeaveRequestRequest request, CancellationToken ct)
        => sender.Send(new CreateLeaveRequest(request.StudentId, request.StartDate, request.EndDate, request.Reason), ct);

    public Task<bool> ReviewLeaveRequestAsync(Guid id, ReviewLeaveRequestRequest request, CancellationToken ct)
        => sender.Send(new ReviewLeaveRequest(id, request.Approve, request.Note), ct);

    public async Task<IReadOnlyList<AttendanceResponse>> GetSessionAttendanceAsync(Guid sessionId, CancellationToken ct)
    {
        var rows = await sender.Send(new GetSessionAttendance(sessionId), ct);
        return rows.Select(r => new AttendanceResponse(r.StudentId, r.Status, r.Method,
            r.CheckedInAt, r.MinutesLate, r.Note)).ToList();
    }
}
