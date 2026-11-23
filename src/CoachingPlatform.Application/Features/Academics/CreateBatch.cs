using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Academics;

public sealed record CreateBatch(
    Guid CourseId, Guid AcademicYearId, Guid? TermId, string Name, string Code,
    DateOnly StartDate, DateOnly EndDate, int Capacity) : IRequest<Guid>, IRequiresTenant;

public sealed class CreateBatchValidator : AbstractValidator<CreateBatch>
{
    public CreateBatchValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.AcademicYearId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 500);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public sealed class CreateBatchHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateBatch, Guid>
{
    public async Task<Guid> Handle(CreateBatch request, CancellationToken ct)
    {
        if (!await db.Set<Course>().AnyAsync(c => c.Id == request.CourseId, ct))
            throw new NotFoundException(nameof(Course), request.CourseId);
        if (!await db.Set<AcademicYear>().AnyAsync(y => y.Id == request.AcademicYearId, ct))
            throw new NotFoundException(nameof(AcademicYear), request.AcademicYearId);
        if (await db.Set<Batch>().AnyAsync(b => b.Code == request.Code, ct))
            throw new ConflictException($"Batch code '{request.Code}' already exists.");

        var batch = new Batch
        {
            TenantId = current.TenantId,
            CourseId = request.CourseId,
            AcademicYearId = request.AcademicYearId,
            TermId = request.TermId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Capacity = request.Capacity,
            Status = BatchStatus.Upcoming
        };
        db.Set<Batch>().Add(batch);
        await db.SaveChangesAsync(ct);
        return batch.Id;
    }
}
