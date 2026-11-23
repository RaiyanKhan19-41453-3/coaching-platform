using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Platform;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Academics;

public sealed record EnrollStudent(Guid BatchId, Guid StudentId) : IRequest<Guid>, IRequiresTenant;

public sealed class EnrollStudentValidator : AbstractValidator<EnrollStudent>
{
    public EnrollStudentValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
    }
}

public sealed class EnrollStudentHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<EnrollStudent, Guid>
{
    public async Task<Guid> Handle(EnrollStudent request, CancellationToken ct)
    {
        var batch = await db.Set<Batch>().FirstOrDefaultAsync(b => b.Id == request.BatchId, ct)
                    ?? throw new NotFoundException(nameof(Batch), request.BatchId);
        if (batch.Status is not (BatchStatus.Upcoming or BatchStatus.Active))
            throw new ConflictException("This batch is not accepting enrolments.");
        if (!await db.Set<User>().AnyAsync(u => u.Id == request.StudentId, ct))
            throw new NotFoundException(nameof(User), request.StudentId);

        if (await db.Set<Enrollment>().AnyAsync(e => e.BatchId == request.BatchId &&
            e.StudentId == request.StudentId &&
            e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Transferred, ct))
            throw new ConflictException("Student is already enrolled in this batch.");

        var active = await db.Set<Enrollment>().CountAsync(e => e.BatchId == request.BatchId &&
            e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Completed, ct);
        if (active >= batch.Capacity)
            throw new ConflictException("Batch is full.");

        var enrollment = new Enrollment
        {
            TenantId = current.TenantId,
            BatchId = batch.Id,
            StudentId = request.StudentId,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAt = DateTimeOffset.UtcNow,
            RollNumber = $"{batch.Code}-{active + 1:000}"
        };
        // Force an UPDATE of the batch row so its concurrency token guards capacity.
        db.Set<Batch>().Entry(batch).Property(b => b.SyllabusVersion).IsModified = true;
        db.Set<Enrollment>().Add(enrollment);
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId,
            EventType = EventTypes.EnrollmentCreated,
            PayloadJson = JsonSerializer.Serialize(new
            {
                enrollment.Id, enrollment.BatchId, enrollment.StudentId, batch.Name
            }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Batch capacity changed concurrently; please retry.");
        }
        return enrollment.Id;
    }
}
