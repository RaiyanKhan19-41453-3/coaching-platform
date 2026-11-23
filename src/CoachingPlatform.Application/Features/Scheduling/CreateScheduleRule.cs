using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Scheduling;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Scheduling;

public sealed record CreateScheduleRule(
    Guid BatchId, int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime,
    DateOnly ValidFrom, DateOnly? ValidTo, Guid? TeacherId, Guid? RoomId, Guid? TopicId, string? RecurrenceRule)
    : IRequest<Guid>, IRequiresTenant;

public sealed class CreateScheduleRuleValidator : AbstractValidator<CreateScheduleRule>
{
    public CreateScheduleRuleValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
        RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
    }
}

public sealed class CreateScheduleRuleHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateScheduleRule, Guid>
{
    public async Task<Guid> Handle(CreateScheduleRule request, CancellationToken ct)
    {
        if (!await db.Set<Batch>().AnyAsync(b => b.Id == request.BatchId, ct))
            throw new NotFoundException(nameof(Batch), request.BatchId);

        var rule = new ScheduleRule
        {
            TenantId = current.TenantId,
            BatchId = request.BatchId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            TeacherId = request.TeacherId,
            RoomId = request.RoomId,
            TopicId = request.TopicId,
            RecurrenceRule = request.RecurrenceRule
        };
        db.Set<ScheduleRule>().Add(rule);
        await db.SaveChangesAsync(ct);
        return rule.Id;
    }
}
