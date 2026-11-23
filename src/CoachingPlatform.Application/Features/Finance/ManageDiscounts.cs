using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Domain.Tenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Finance;

public sealed record CreateDiscount(string Code, string Name, DiscountType Type, decimal Value,
    decimal? MaxDiscountAmount, Guid? CourseId, DateOnly ValidFrom, DateOnly? ValidTo, int? UsageLimit)
    : IRequest<Guid>, IRequiresTenant;

public sealed class CreateDiscountValidator : AbstractValidator<CreateDiscount>
{
    public CreateDiscountValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Value).GreaterThan(0);
    }
}

public sealed class CreateDiscountHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateDiscount, Guid>
{
    public async Task<Guid> Handle(CreateDiscount request, CancellationToken ct)
    {
        if (await db.Set<Discount>().AnyAsync(d => d.Code == request.Code, ct))
            throw new ConflictException("Discount code already exists.");
        var tenant = await db.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == current.TenantId, ct)
                    ?? throw new NotFoundException(nameof(Tenant), current.TenantId);
        var discount = new Discount
        {
            TenantId = current.TenantId,
            IsActive = true, Code = request.Code, Name = request.Name,
            Type = request.Type, Value = request.Value,
            MaxDiscountAmount = request.MaxDiscountAmount is { } max
                ? new Money(max, tenant.DefaultCurrency) : Money.Zero(tenant.DefaultCurrency),
            CourseId = request.CourseId, ValidFrom = request.ValidFrom, ValidTo = request.ValidTo,
            UsageLimit = request.UsageLimit
        };
        db.Set<Discount>().Add(discount);
        await db.SaveChangesAsync(ct);
        return discount.Id;
    }
}

public sealed record CreateScholarship(Guid StudentId, string Name, DiscountType Type, decimal Value,
    DateOnly StartDate, DateOnly? EndDate) : IRequest<Guid>, IRequiresTenant;

public sealed class CreateScholarshipValidator : AbstractValidator<CreateScholarship>
{
    public CreateScholarshipValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Value).GreaterThan(0);
    }
}

public sealed class CreateScholarshipHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateScholarship, Guid>
{
    public async Task<Guid> Handle(CreateScholarship request, CancellationToken ct)
    {
        var scholarship = new Scholarship
        {
            TenantId = current.TenantId, StudentId = request.StudentId, IsActive = true, Name = request.Name,
            Type = request.Type, Value = request.Value, StartDate = request.StartDate,
            EndDate = request.EndDate, ApprovedBy = current.UserId
        };
        db.Set<Scholarship>().Add(scholarship);
        await db.SaveChangesAsync(ct);
        return scholarship.Id;
    }
}
