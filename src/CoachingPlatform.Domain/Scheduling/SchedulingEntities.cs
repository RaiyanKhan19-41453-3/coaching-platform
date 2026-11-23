using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;

namespace CoachingPlatform.Domain.Scheduling;

public sealed class Room : TenantEntity
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public ResourceType Type { get; set; } = ResourceType.Classroom;
    public int Capacity { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public bool IsActive { get; set; } = true;
    public string? EquipmentJson { get; set; }
}

/// <summary>
/// Recurring rule for a batch (e.g. "Mon/Wed/Fri 18:00-20:00 until 2027-03-01").
/// Concrete ClassSession rows are materialised from this rule by the scheduling service.
/// </summary>
public sealed class ScheduleRule : TenantEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = default!;
    public Guid? TeacherId { get; set; }
    public User? Teacher { get; set; }
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }
    public Guid? TopicId { get; set; }
    public int DayOfWeek { get; set; }                 // 0 = Sunday
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? RecurrenceRule { get; set; }       // RFC 5545 RRULE
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>A single occurrence of a class. Materialised instances make attendance simple.</summary>
public sealed class ClassSession : TenantEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = default!;
    public Guid? ScheduleRuleId { get; set; }
    public ScheduleRule? ScheduleRule { get; set; }
    public Guid? TeacherId { get; set; }
    public User? Teacher { get; set; }
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }
    public Guid? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public string? Title { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? RescheduledFromSessionId { get; set; }
    public string? MeetingUrl { get; set; }

    public ICollection<Attendance> Attendance { get; set; } = [];
}

/// <summary>One student's attendance for one session. Unique on (SessionId, StudentId).</summary>
public sealed class Attendance : TenantEntity
{
    public Guid SessionId { get; set; }
    public ClassSession Session { get; set; } = default!;
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.NotMarked;
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Manual;
    public DateTimeOffset? CheckedInAt { get; set; }
    public int MinutesLate { get; set; }
    public Guid? MarkedBy { get; set; }
    public string? Note { get; set; }
}

public sealed class LeaveRequest : TenantEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid? SessionId { get; set; }
    public ClassSession? Session { get; set; }
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = default!;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public Guid RequestedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
