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

public sealed record GradeAttemptAnswer(Guid AttemptId, Guid QuestionId, decimal MarksAwarded, string? Feedback)
    : IRequest<bool>, IRequiresTenant;

public sealed class GradeAttemptAnswerValidator : AbstractValidator<GradeAttemptAnswer>
{
    public GradeAttemptAnswerValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.MarksAwarded).GreaterThanOrEqualTo(0);
    }
}

public sealed class GradeAttemptAnswerHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<GradeAttemptAnswer, bool>
{
    public async Task<bool> Handle(GradeAttemptAnswer request, CancellationToken ct)
    {
        var attempt = await db.Set<AssessmentAttempt>()
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct)
            ?? throw new NotFoundException(nameof(AssessmentAttempt), request.AttemptId);
        var answer = await db.Set<StudentAnswer>()
            .FirstOrDefaultAsync(a => a.AttemptId == attempt.Id && a.QuestionId == request.QuestionId, ct)
            ?? throw new NotFoundException(nameof(StudentAnswer), request.QuestionId);

        var link = await db.Set<AssessmentQuestion>()
            .FirstOrDefaultAsync(x => x.AssessmentId == attempt.AssessmentId && x.QuestionId == request.QuestionId, ct);
        var max = link?.Marks ?? 1m;
        if (request.MarksAwarded > max) throw new ConflictException("Awarded marks exceed the question maximum.");

        answer.MarksAwarded = request.MarksAwarded;
        answer.IsCorrect = request.MarksAwarded == max;
        answer.Feedback = request.Feedback;
        answer.GradedBy = current.UserId;
        answer.GradedAt = DateTimeOffset.UtcNow;

        var answers = await db.Set<StudentAnswer>().Where(a => a.AttemptId == attempt.Id)
            .Select(a => a.MarksAwarded).ToListAsync(ct);
        attempt.Score = answers.Sum();
        attempt.Status = AttemptStatus.Graded;
        attempt.SubmittedAt ??= DateTimeOffset.UtcNow;

        var grades = await db.Set<GradeEntry>().Where(g =>
            g.AssessmentId == attempt.AssessmentId && g.StudentId == attempt.StudentId).ToListAsync(ct);
        foreach (var grade in grades)
        {
            grade.MarksObtained = attempt.Score ?? 0m;
            grade.GradedAt = DateTimeOffset.UtcNow;
        }

        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId, EventType = EventTypes.AssessmentGraded,
            PayloadJson = JsonSerializer.Serialize(new
            { attempt.Id, attempt.AssessmentId, attempt.StudentId, Score = attempt.Score }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return true;
    }
}
