using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Domain.Scheduling;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Scheduling;

/// <summary>
/// Materialises recurring rules into concrete ClassSession rows so attendance, chat and
/// billing attach to instances rather than to a recurrence pattern.
/// </summary>
public sealed record GenerateSessions(Guid ScheduleRuleId, DateOnly From, DateOnly To) : IRequest<int>, IRequiresTenant;

public sealed class GenerateSessionsValidator : AbstractValidator<GenerateSessions>
{
    public GenerateSessionsValidator()
    {
        RuleFor(x => x.ScheduleRuleId).NotEmpty();
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x).Must(x => (x.To.ToDateTime(TimeOnly.MinValue) - x.From.ToDateTime(TimeOnly.MinValue)).Days <= 366)
            .WithMessage("Generate at most one year of sessions per call.");
    }
}

public sealed class GenerateSessionsHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<GenerateSessions, int>
{
    public async Task<int> Handle(GenerateSessions request, CancellationToken ct)
    {
        var rule = await db.Set<ScheduleRule>().FirstOrDefaultAsync(r => r.Id == request.ScheduleRuleId, ct)
                   ?? throw new NotFoundException(nameof(ScheduleRule), request.ScheduleRuleId);

        var from = request.From < rule.ValidFrom ? rule.ValidFrom : request.From;
        var to = rule.ValidTo.HasValue && request.To > rule.ValidTo.Value ? rule.ValidTo.Value : request.To;
        if (from > to) return 0;

        var existingStarts = await db.Set<ClassSession>()
            .Where(s => s.ScheduleRuleId == rule.Id && s.StartUtc >= from.ToDateTime(TimeOnly.MinValue))
            .Select(s => s.StartUtc).ToListAsync(ct);
        var known = existingStarts.ToHashSet();

        var created = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if ((int)date.DayOfWeek != rule.DayOfWeek) continue;
            var start = new DateTimeOffset(date.ToDateTime(rule.StartTime, DateTimeKind.Utc));
            if (known.Contains(start)) continue;

            // Conflict detection: same teacher or room already booked in an overlapping window.
            var end = new DateTimeOffset(date.ToDateTime(rule.EndTime, DateTimeKind.Utc));
            var conflict = await db.Set<ClassSession>().AnyAsync(s =>
                !s.IsCancelled && s.StartUtc < end && s.EndUtc > start &&
                ((rule.TeacherId.HasValue && s.TeacherId == rule.TeacherId) ||
                 (rule.RoomId.HasValue && s.RoomId == rule.RoomId)), ct);
            if (conflict) throw new ConflictException($"Resource conflict on {date:yyyy-MM-dd}.");

            db.Set<ClassSession>().Add(new ClassSession
            {
                TenantId = current.TenantId,
                BatchId = rule.BatchId,
                ScheduleRuleId = rule.Id,
                TeacherId = rule.TeacherId,
                RoomId = rule.RoomId,
                TopicId = rule.TopicId,
                Title = $"Class {date:yyyy-MM-dd}",
                StartUtc = start,
                EndUtc = end,
                Status = SessionStatus.Scheduled
            });
            created++;
        }

        if (created > 0)
        {
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                TenantId = current.TenantId,
                EventType = EventTypes.SessionsGenerated,
                PayloadJson = JsonSerializer.Serialize(new { rule.Id, rule.BatchId, Count = created }),
                OccurredAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        return created;
    }
}
