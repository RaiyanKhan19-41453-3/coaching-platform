using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Crm;

public sealed record CreateLeadRequest(string FirstName, string LastName, string? Email, string? PhoneNumber,
    LeadSource Source, Guid? AssignedToId, Guid? CourseId, string? Notes);
public sealed record UpdateLeadStatusRequest(LeadStatus Status, string? Notes);
public sealed record CreateApplicationRequest(Guid? LeadId, Guid CourseId, Guid? AcademicYearId,
    string FirstName, string LastName, string? Email, string? PhoneNumber,
    string? GuardianName, string? GuardianPhone);
public sealed record AddApplicationDocumentRequest(string DocumentType, string FileName, string StorageKey);
public sealed record ReviewApplicationRequest(ApplicationStatus Status, string? Notes, decimal? TestScore);
public sealed record EnrollApplicantRequest(Guid BatchId, string? Password, Guid? GuardianUserId);
public sealed record CreateFollowUpRequest(Guid? LeadId, Guid? ApplicationId, string Note, DateTimeOffset ScheduledAt);
public sealed record LeadResponse(Guid Id, string Name, string? Email, LeadStatus Status, DateTimeOffset CreatedAt);
public sealed record ApplicationResponse(Guid Id, string Name, Guid CourseId,
    ApplicationStatus Status, DateTimeOffset AppliedAt);

public sealed record ChildResponse(Guid StudentId, string FirstName, string LastName, string Relationship);
public sealed record ChildSummaryResponse(Guid StudentId, string Name, decimal AttendancePercent,
    decimal Collected, decimal Outstanding, decimal AverageGrade, int ActiveEnrollments);
