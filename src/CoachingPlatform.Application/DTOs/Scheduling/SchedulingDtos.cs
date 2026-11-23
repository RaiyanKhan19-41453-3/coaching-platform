using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Scheduling;

public sealed record CreateScheduleRuleRequest(
    Guid BatchId, int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime,
    DateOnly ValidFrom, DateOnly? ValidTo, Guid? TeacherId, Guid? RoomId,
    Guid? TopicId, string? RecurrenceRule);

public sealed record GenerateSessionsRequest(DateOnly From, DateOnly To);

public sealed record AttendanceMarkItem(
    Guid StudentId, AttendanceStatus Status, int MinutesLate = 0, string? Note = null);

public sealed record MarkAttendanceRequest(IReadOnlyList<AttendanceMarkItem> Records);

public sealed record AttendanceResponse(
    Guid StudentId, AttendanceStatus Status, AttendanceMethod Method,
    DateTimeOffset? CheckedInAt, int MinutesLate, string? Note);
