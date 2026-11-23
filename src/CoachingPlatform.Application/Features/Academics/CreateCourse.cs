using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Academics;

public sealed record CreateCourse(
    string Code, string Name, string? Description, CourseLevel Level, int DurationMonths) : IRequest<Guid>, IRequiresTenant;

public sealed class CreateCourseValidator : AbstractValidator<CreateCourse>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DurationMonths).InclusiveBetween(1, 120);
    }
}

public sealed class CreateCourseHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateCourse, Guid>
{
    public async Task<Guid> Handle(CreateCourse request, CancellationToken ct)
    {
        if (await db.Set<Course>().AnyAsync(c => c.Code == request.Code, ct))
            throw new ConflictException($"Course code '{request.Code}' already exists in this center.");

        var course = new Course
        {
            TenantId = current.TenantId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Level = request.Level,
            DurationMonths = request.DurationMonths,
            IsActive = true
        };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync(ct);
        return course.Id;
    }
}

public sealed record CourseDto(Guid Id, string Code, string Name, CourseLevel Level, int DurationMonths);
