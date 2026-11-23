using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Assessment;

public sealed record CreateQuestion(Guid SubjectId, Guid? TopicId, QuestionType Type, Difficulty Difficulty,
    string Text, string? OptionsJson, string? CorrectAnswerJson, string? Explanation,
    decimal Marks, decimal? NegativeMarks, string? TagsJson) : IRequest<Guid>, IRequiresTenant;

public sealed class CreateQuestionValidator : AbstractValidator<CreateQuestion>
{
    public CreateQuestionValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.Marks).GreaterThan(0);
    }
}

public sealed class CreateQuestionHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateQuestion, Guid>
{
    public async Task<Guid> Handle(CreateQuestion request, CancellationToken ct)
    {
        var question = new Question
        {
            TenantId = current.TenantId, SubjectId = request.SubjectId, TopicId = request.TopicId,
            Type = request.Type, Difficulty = request.Difficulty, Text = request.Text,
            OptionsJson = request.OptionsJson, CorrectAnswerJson = request.CorrectAnswerJson,
            Explanation = request.Explanation, Marks = request.Marks,
            NegativeMarks = request.NegativeMarks, TagsJson = request.TagsJson,
            AuthorId = current.UserId
        };
        db.Set<Question>().Add(question);
        await db.SaveChangesAsync(ct);
        return question.Id;
    }
}

public sealed record UpdateQuestion(Guid QuestionId, string Text, string? OptionsJson,
    string? CorrectAnswerJson, string? Explanation, decimal Marks) : IRequest<bool>, IRequiresTenant;

public sealed class UpdateQuestionHandler(IApplicationDbContext db) : IRequestHandler<UpdateQuestion, bool>
{
    public async Task<bool> Handle(UpdateQuestion request, CancellationToken ct)
    {
        var question = await db.Set<Question>().FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct)
                       ?? throw new NotFoundException(nameof(Question), request.QuestionId);
        question.Text = request.Text;
        question.OptionsJson = request.OptionsJson;
        question.CorrectAnswerJson = request.CorrectAnswerJson;
        question.Explanation = request.Explanation;
        question.Marks = request.Marks;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record AddQuestionToAssessment(Guid AssessmentId, Guid QuestionId, decimal? Marks, int Order)
    : IRequest<Guid>, IRequiresTenant;

public sealed class AddQuestionToAssessmentHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<AddQuestionToAssessment, Guid>
{
    public async Task<Guid> Handle(AddQuestionToAssessment request, CancellationToken ct)
    {
        if (!await db.Set<Domain.Assessment.Assessment>().AnyAsync(a => a.Id == request.AssessmentId, ct))
            throw new NotFoundException("Assessment", request.AssessmentId);
        if (!await db.Set<Question>().AnyAsync(q => q.Id == request.QuestionId, ct))
            throw new NotFoundException(nameof(Question), request.QuestionId);
        if (await db.Set<AssessmentQuestion>().AnyAsync(x =>
                x.AssessmentId == request.AssessmentId && x.QuestionId == request.QuestionId, ct))
            throw new ConflictException("Question is already attached to this assessment.");
        var link = new AssessmentQuestion
        {
            TenantId = current.TenantId, AssessmentId = request.AssessmentId,
            QuestionId = request.QuestionId, Marks = request.Marks ?? 1m, Order = request.Order
        };
        db.Set<AssessmentQuestion>().Add(link);
        await db.SaveChangesAsync(ct);
        return link.Id;
    }
}

public sealed record ListQuestions(Guid? SubjectId, Guid? TopicId) : IRequest<IReadOnlyList<QuestionDto>>, IRequiresTenant;

public sealed record QuestionDto(Guid Id, Guid SubjectId, QuestionType Type, Difficulty Difficulty,
    string Text, decimal Marks);

public sealed class ListQuestionsHandler(IApplicationDbContext db) : IRequestHandler<ListQuestions, IReadOnlyList<QuestionDto>>
{
    public async Task<IReadOnlyList<QuestionDto>> Handle(ListQuestions request, CancellationToken ct)
    {
        var query = db.Set<Question>().AsNoTracking().AsQueryable();
        if (request.SubjectId.HasValue) query = query.Where(q => q.SubjectId == request.SubjectId.Value);
        if (request.TopicId.HasValue) query = query.Where(q => q.TopicId == request.TopicId.Value);
        return await query.OrderBy(q => q.Text)
            .Select(q => new QuestionDto(q.Id, q.SubjectId, q.Type, q.Difficulty, q.Text, q.Marks))
            .ToListAsync(ct);
    }
}
