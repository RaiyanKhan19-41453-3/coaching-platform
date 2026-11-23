using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Academics;

public sealed record CreateCourseRequest(
    string Code, string Name, string? Description, CourseLevel Level, int DurationMonths);

public sealed record CreateBatchRequest(
    Guid CourseId, Guid AcademicYearId, Guid? TermId, string Name, string Code,
    DateOnly StartDate, DateOnly EndDate, int Capacity);

public sealed record BatchQuery(BatchStatus? Status, string? Search, int Page, int PageSize);

public sealed record BatchResponse(
    Guid Id, string Code, string Name, BatchStatus Status,
    DateOnly StartDate, DateOnly EndDate, int Capacity);

public sealed record EnrollStudentRequest(Guid StudentId);
