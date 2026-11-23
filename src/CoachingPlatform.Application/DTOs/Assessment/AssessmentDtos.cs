using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Assessment;

public sealed record CreateAssessmentRequest(
    Guid SubjectId, Guid? BatchId, AssessmentType Type, string Title, string? Instructions,
    DateTimeOffset? StartUtc, DateTimeOffset? EndUtc, int? DurationMinutes,
    decimal TotalMarks, decimal? PassingMarks);

public sealed record AnswerInputDto(Guid QuestionId, string? ResponseJson);

public sealed record SubmitAttemptRequest(Guid StudentId, IReadOnlyList<AnswerInputDto> Answers);

public sealed record CreateQuestionRequest(Guid SubjectId, Guid? TopicId, QuestionType Type, Difficulty Difficulty,
    string Text, string? OptionsJson, string? CorrectAnswerJson, string? Explanation,
    decimal Marks, decimal? NegativeMarks, string? TagsJson);

public sealed record UpdateQuestionRequest(string Text, string? OptionsJson,
    string? CorrectAnswerJson, string? Explanation, decimal Marks);

public sealed record AddQuestionRequest(Guid QuestionId, decimal? Marks, int Order);

public sealed record QuestionResponse(Guid Id, Guid SubjectId, QuestionType Type,
    Difficulty Difficulty, string Text, decimal Marks);

public sealed record GradeAnswerRequest(Guid QuestionId, decimal MarksAwarded, string? Feedback);
