using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Platform;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Academics;

public sealed record ChangeEnrollmentStatus(Guid EnrollmentId, EnrollmentStatus Status, string? Reason)
    : IRequest<bool>, IRequiresTenant;

public sealed class ChangeEnrollmentStatusValidator : AbstractValidator<ChangeEnrollmentStatus>
{
    public ChangeEnrollmentStatusValidator() => RuleFor(x => x.EnrollmentId).NotEmpty();
}

public sealed class ChangeEnrollmentStatusHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<ChangeEnrollmentStatus, bool>
{
    public async Task<bool> Handle(ChangeEnrollmentStatus request, CancellationToken ct)
    {
        var enrollment = await db.Set<Enrollment>().FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, ct)
                         ?? throw new NotFoundException(nameof(Enrollment), request.EnrollmentId);
        if (enrollment.Status == request.Status) return false;

        if (request.Status == EnrollmentStatus.Enrolled &&
            enrollment.Status is EnrollmentStatus.Waitlisted or EnrollmentStatus.Applied)
        {
            var batch = await db.Set<Batch>().FirstOrDefaultAsync(b => b.Id == enrollment.BatchId, ct)
                        ?? throw new NotFoundException(nameof(Batch), enrollment.BatchId);
            var active = await db.Set<Enrollment>().CountAsync(e => e.BatchId == batch.Id &&
                e.Id != enrollment.Id && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed), ct);
            if (active >= batch.Capacity) throw new ConflictException("Batch is full; cannot promote.");
            db.Set<Batch>().Entry(batch).Property(b => b.SyllabusVersion).IsModified = true;
        }

        enrollment.Status = request.Status;
        if (request.Status == EnrollmentStatus.Completed) enrollment.CompletedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId, EventType = EventTypes.EnrollmentStatusChanged,
            PayloadJson = JsonSerializer.Serialize(new
            {
                enrollment.Id, enrollment.BatchId, enrollment.StudentId, Status = request.Status.ToString()
            }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Enrollment changed concurrently; retry."); }
        return true;
    }
}

public sealed record AssignTeacher(Guid BatchId, Guid TeacherId, bool IsLead)
    : IRequest<Guid>, IRequiresTenant;

public sealed class AssignTeacherValidator : AbstractValidator<AssignTeacher>
{
    public AssignTeacherValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}

public sealed class AssignTeacherHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<AssignTeacher, Guid>
{
    public async Task<Guid> Handle(AssignTeacher request, CancellationToken ct)
    {
        if (!await db.Set<Batch>().AnyAsync(b => b.Id == request.BatchId, ct))
            throw new NotFoundException(nameof(Batch), request.BatchId);
        if (!await db.Set<Domain.Identity.User>().AnyAsync(u => u.Id == request.TeacherId, ct))
            throw new NotFoundException(nameof(Domain.Identity.User), request.TeacherId);
        if (await db.Set<BatchTeacher>().AnyAsync(t => t.BatchId == request.BatchId && t.TeacherId == request.TeacherId, ct))
            throw new ConflictException("Teacher is already assigned to this batch.");
        var assignment = new BatchTeacher
        {
            TenantId = current.TenantId, BatchId = request.BatchId,
            TeacherId = request.TeacherId, IsLead = request.IsLead,
            AssignedAt = DateTimeOffset.UtcNow
        };
        db.Set<BatchTeacher>().Add(assignment);
        await db.SaveChangesAsync(ct);
        return assignment.Id;
    }
}

public sealed record RemoveTeacher(Guid BatchId, Guid TeacherId) : IRequest<bool>, IRequiresTenant;

public sealed class RemoveTeacherHandler(IApplicationDbContext db) : IRequestHandler<RemoveTeacher, bool>
{
    public async Task<bool> Handle(RemoveTeacher request, CancellationToken ct)
    {
        var assignment = await db.Set<BatchTeacher>()
            .FirstOrDefaultAsync(t => t.BatchId == request.BatchId && t.TeacherId == request.TeacherId, ct)
            ?? throw new NotFoundException(nameof(BatchTeacher), request.TeacherId);
        db.Set<BatchTeacher>().Remove(assignment);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
