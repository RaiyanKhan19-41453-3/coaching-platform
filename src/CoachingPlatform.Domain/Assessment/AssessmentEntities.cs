using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;

namespace CoachingPlatform.Domain.Assessment;

public sealed class Question : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;
    public Guid? TopicId { get; set; }
    public Topic? Topic { get; set; }
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;
    public string Text { get; set; } = default!;
    public string? OptionsJson { get; set; }        // [{ "id":"a", "text":"..." }]
    public string? CorrectAnswerJson { get; set; }  // works for multi-select / numeric tolerance too
    public string? Explanation { get; set; }
    public decimal Marks { get; set; } = 1m;
    public decimal? NegativeMarks { get; set; }
    public string? TagsJson { get; set; }
    public Guid? AuthorId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Assessment : TenantEntity
{
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;
    public AssessmentType Type { get; set; } = AssessmentType.Quiz;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public DateTimeOffset? StartUtc { get; set; }
    public DateTimeOffset? EndUtc { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal? PassingMarks { get; set; }
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ShowResultsAt { get; set; }
    public bool AllowRetakes { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public bool RandomizeQuestions { get; set; }
    public bool IsProctored { get; set; }
    public Guid? AuthorId { get; set; }

    public ICollection<AssessmentQuestion> Questions { get; set; } = [];
}

public sealed class AssessmentQuestion : TenantEntity
{
    public Guid AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = default!;
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = default!;
    public int Order { get; set; }
    public decimal Marks { get; set; } = 1m;
    public bool IsCompulsory { get; set; } = true;
}

public sealed class AssessmentAttempt : TenantEntity
{
    public Guid AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = default!;
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public int AttemptNumber { get; set; } = 1;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public int TabSwitchCount { get; set; }
    public bool IsFlagged { get; set; }
    public string? ProctorNotes { get; set; }
    public string? IpAddress { get; set; }

    public ICollection<StudentAnswer> Answers { get; set; } = [];
}

public sealed class StudentAnswer : TenantEntity
{
    public Guid AttemptId { get; set; }
    public AssessmentAttempt Attempt { get; set; } = default!;
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = default!;
    public string? ResponseJson { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedBy { get; set; }
    public DateTimeOffset? GradedAt { get; set; }
}

/// <summary>Denormalised gradebook row. Feeds report cards, rank and analytics fast.</summary>
public sealed class GradeEntry : TenantEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid? BatchId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? AssessmentId { get; set; }
    public Guid? TermId { get; set; }
    public GradeCategory Category { get; set; }
    public string Title { get; set; } = default!;
    public decimal MarksObtained { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal WeightPercent { get; set; } = 100m;
    public Guid? GradedBy { get; set; }
    public DateTimeOffset? GradedAt { get; set; }
}

public sealed class Rubric : TenantEntity
{
    public Guid? AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public string Title { get; set; } = default!;
    public decimal MaxScore { get; set; }
    public ICollection<RubricCriterion> Criteria { get; set; } = [];
}

public sealed class RubricCriterion : TenantEntity
{
    public Guid RubricId { get; set; }
    public Rubric Rubric { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal MaxScore { get; set; }
    public int Order { get; set; }
}
