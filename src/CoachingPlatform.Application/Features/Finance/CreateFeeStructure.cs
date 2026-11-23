using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Finance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Finance;

public sealed record CreateFeeStructure(Guid? CourseId, Guid? BatchId, string Name, string? Code,
    decimal Amount, string Currency, FeeFrequency Frequency, decimal TaxPercent,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo) : IRequest<Guid>, IRequiresTenant;

public sealed class CreateFeeStructureValidator : AbstractValidator<CreateFeeStructure>
{
    public CreateFeeStructureValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).Length(3);
        RuleFor(x => x.TaxPercent).InclusiveBetween(0m, 100m);
    }
}

public sealed class CreateFeeStructureHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateFeeStructure, Guid>
{
    public async Task<Guid> Handle(CreateFeeStructure request, CancellationToken ct)
    {
        if (request.CourseId.HasValue && !await db.Set<Domain.Academics.Course>().AnyAsync(c => c.Id == request.CourseId.Value, ct))
            throw new NotFoundException("Course", request.CourseId.Value);
        if (request.BatchId.HasValue && !await db.Set<Domain.Academics.Batch>().AnyAsync(b => b.Id == request.BatchId.Value, ct))
            throw new NotFoundException("Batch", request.BatchId.Value);

        var fee = new FeeStructure
        {
            TenantId = current.TenantId, IsActive = true, CourseId = request.CourseId, BatchId = request.BatchId,
            Name = request.Name, Code = request.Code, Amount = new Money(request.Amount, request.Currency),
            Frequency = request.Frequency, TaxPercent = request.TaxPercent,
            EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo
        };
        db.Set<FeeStructure>().Add(fee);
        await db.SaveChangesAsync(ct);
        return fee.Id;
    }
}
