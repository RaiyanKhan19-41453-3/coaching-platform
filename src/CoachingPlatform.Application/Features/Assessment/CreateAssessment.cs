using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Assessment;

public sealed record CreateAssessment(
    Guid SubjectId, Guid? BatchId, AssessmentType Type, string Title, string? Instructions,
    DateTimeOffset? StartUtc, DateTimeOffset? EndUtc, int? DurationMinutes,
    decimal TotalMarks, decimal? PassingMarks) : IRequest<Guid>, IRequiresTenant;

public sealed class CreateAssessmentValidator : AbstractValidator<CreateAssessment>
{
    public CreateAssessmentValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TotalMarks).GreaterThan(0);
        RuleFor(x => x.EndUtc).GreaterThan(x => x.StartUtc!.Value)
            .When(x => x.StartUtc.HasValue && x.EndUtc.HasValue);
    }
}

public sealed class CreateAssessmentHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateAssessment, Guid>
{
    public async Task<Guid> Handle(CreateAssessment request, CancellationToken ct)
    {
        if (!await db.Set<Subject>().AnyAsync(s => s.Id == request.SubjectId, ct))
            throw new NotFoundException(nameof(Subject), request.SubjectId);

        var assessment = new Domain.Assessment.Assessment
        {
            TenantId = current.TenantId,
            SubjectId = request.SubjectId,
            BatchId = request.BatchId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Instructions = request.Instructions,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            DurationMinutes = request.DurationMinutes,
            TotalMarks = request.TotalMarks,
            PassingMarks = request.PassingMarks,
            Status = AssessmentStatus.Draft,
            MaxAttempts = 1,
            AuthorId = current.UserId
        };
        db.Set<Domain.Assessment.Assessment>().Add(assessment);
        await db.SaveChangesAsync(ct);
        return assessment.Id;
    }
}
