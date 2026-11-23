using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Assessment;

public sealed record CreateAssessmentRequest(
    Guid SubjectId, Guid? BatchId, AssessmentType Type, string Title, string? Instructions,
    DateTimeOffset? StartUtc, DateTimeOffset? EndUtc, int? DurationMinutes,
    decimal TotalMarks, decimal? PassingMarks);

public sealed record AnswerInputDto(Guid QuestionId, string? ResponseJson);

public sealed record SubmitAttemptRequest(Guid StudentId, IReadOnlyList<AnswerInputDto> Answers);
