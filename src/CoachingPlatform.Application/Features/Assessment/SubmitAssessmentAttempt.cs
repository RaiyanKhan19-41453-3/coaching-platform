using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Platform;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Assessment;

public sealed record AnswerInput(Guid QuestionId, string? ResponseJson);
public sealed record SubmitAttempt(Guid AssessmentId, Guid StudentId, IReadOnlyList<AnswerInput> Answers)
    : IRequest<Guid>, IRequiresTenant;

public sealed class SubmitAttemptValidator : AbstractValidator<SubmitAttempt>
{
    public SubmitAttemptValidator()
    {
        RuleFor(x => x.AssessmentId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Answers).NotNull();
    }
}

/// <summary>
/// Auto-grades objective question types on submit and writes a denormalised gradebook row.
/// Essay/coding answers remain ungraded for a human marker.
/// </summary>
public sealed class SubmitAttemptHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<SubmitAttempt, Guid>
{
    public async Task<Guid> Handle(SubmitAttempt request, CancellationToken ct)
    {
        var assessment = await db.Set<Domain.Assessment.Assessment>()
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, ct)
            ?? throw new NotFoundException(nameof(Domain.Assessment.Assessment), request.AssessmentId);
        if (assessment.Status is AssessmentStatus.Draft or AssessmentStatus.Archived)
            throw new ConflictException("Assessment is not accepting submissions.");

        var attemptNo = await db.Set<AssessmentAttempt>()
            .CountAsync(a => a.AssessmentId == assessment.Id && a.StudentId == request.StudentId, ct) + 1;
        if (attemptNo > assessment.MaxAttempts)
            throw new ConflictException("Maximum number of attempts reached.");

        var questionIds = request.Answers.Select(a => a.QuestionId).ToList();
        var questions = await db.Set<Question>().Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, ct);
        var linkMarks = await db.Set<AssessmentQuestion>()
            .Where(x => x.AssessmentId == assessment.Id && questionIds.Contains(x.QuestionId))
            .ToDictionaryAsync(x => x.QuestionId, x => x.Marks, ct);

        var attempt = new AssessmentAttempt
        {
            TenantId = current.TenantId,
            AssessmentId = assessment.Id,
            StudentId = request.StudentId,
            AttemptNumber = attemptNo,
            StartedAt = DateTimeOffset.UtcNow,
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = AttemptStatus.Graded,
            MaxScore = assessment.TotalMarks
        };

        decimal score = 0;
        decimal maxPossible = 0;
        foreach (var answer in request.Answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var question))
                throw new NotFoundException(nameof(Question), answer.QuestionId);
            var marks = linkMarks.GetValueOrDefault(answer.QuestionId, question.Marks);
            maxPossible += marks;

            var (isCorrect, awarded, autoGraded) = Grade(question, answer.ResponseJson, marks);
            if (!autoGraded) attempt.Status = AttemptStatus.Grading;
            score += awarded;
            attempt.Answers.Add(new StudentAnswer
            {
                TenantId = current.TenantId,
                QuestionId = question.Id,
                ResponseJson = answer.ResponseJson,
                IsCorrect = autoGraded ? isCorrect : null,
                MarksAwarded = awarded
            });
        }

        attempt.Score = score;
        attempt.MaxScore = maxPossible;
        db.Set<AssessmentAttempt>().Add(attempt);
        db.Set<GradeEntry>().Add(new GradeEntry
        {
            TenantId = current.TenantId,
            StudentId = request.StudentId,
            BatchId = assessment.BatchId,
            SubjectId = assessment.SubjectId,
            AssessmentId = assessment.Id,
            TermId = null,
            Category = assessment.Type == AssessmentType.Assignment ? GradeCategory.Assignment
                     : assessment.Type == AssessmentType.Quiz ? GradeCategory.Quiz : GradeCategory.Exam,
            Title = assessment.Title,
            MarksObtained = score,
            MaxMarks = maxPossible,
            GradedBy = current.UserId,
            GradedAt = DateTimeOffset.UtcNow
        });
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId,
            EventType = EventTypes.AssessmentSubmitted,
            PayloadJson = JsonSerializer.Serialize(new
            { AttemptId = attempt.Id, AssessmentId = assessment.Id, assessment.SubjectId, request.StudentId, Score = score, MaxScore = maxPossible }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return attempt.Id;
    }

    private static (bool? Correct, decimal Awarded, bool AutoGraded) Grade(
        Question question, string? response, decimal marks)
    {
        if (question.Type is QuestionType.Essay or QuestionType.Coding ||
            string.IsNullOrWhiteSpace(question.CorrectAnswerJson) ||
            string.IsNullOrWhiteSpace(response))
            return (null, 0m, false);

        try
        {
            using var expectedDoc = JsonDocument.Parse(question.CorrectAnswerJson);
            using var actualDoc = JsonDocument.Parse(response);
            var expected = expectedDoc.RootElement;
            var actual = actualDoc.RootElement;

            bool correct = question.Type switch
            {
                QuestionType.SingleChoice or QuestionType.TrueFalse =>
                    expected.GetProperty("value").GetRawText() == actual.GetProperty("value").GetRawText(),
                QuestionType.MultipleChoice =>
                    expected.GetProperty("values").EnumerateArray().Select(x => x.GetString()).OrderBy(x => x)
                        .SequenceEqual(actual.GetProperty("values").EnumerateArray().Select(x => x.GetString()).OrderBy(x => x)),
                QuestionType.Numeric => Math.Abs(expected.GetProperty("value").GetDecimal() - actual.GetProperty("value").GetDecimal())
                    <= (expected.TryGetProperty("tolerance", out var t) ? t.GetDecimal() : 0.01m),
                _ => false
            };
            if (!correct && question.NegativeMarks.HasValue) return (false, -question.NegativeMarks.Value, true);
            return (correct, correct ? marks : 0m, true);
        }
        catch (JsonException)
        {
            return (null, 0m, false);
        }
    }
}
