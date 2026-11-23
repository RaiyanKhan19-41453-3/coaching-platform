namespace CoachingPlatform.Application.DTOs.Crm;

public sealed record ChildResponse(Guid StudentId, string FirstName, string LastName, string Relationship);

public sealed record ChildSummaryResponse(Guid StudentId, string Name, decimal AttendancePercent,
    decimal Collected, decimal Outstanding, decimal AverageGrade, int ActiveEnrollments);
