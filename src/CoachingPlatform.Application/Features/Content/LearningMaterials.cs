using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Content;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Content;

public sealed record AttachLearningMaterial(Guid? BatchId, Guid? TopicId, string Title,
    MaterialType Type, Guid? AssetId, string? ExternalUrl, int? DurationMinutes, int Order)
    : IRequest<Guid>, IRequiresTenant;

public sealed class AttachLearningMaterialValidator : AbstractValidator<AttachLearningMaterial>
{
    public AttachLearningMaterialValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x).Must(x => x.AssetId.HasValue || !string.IsNullOrWhiteSpace(x.ExternalUrl))
            .WithMessage("An asset or an external URL is required.");
    }
}

public sealed class AttachLearningMaterialHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<AttachLearningMaterial, Guid>
{
    public async Task<Guid> Handle(AttachLearningMaterial request, CancellationToken ct)
    {
        var asset = request.AssetId.HasValue
            ? await db.Set<Asset>().FirstOrDefaultAsync(a => a.Id == request.AssetId.Value, ct)
              ?? throw new NotFoundException(nameof(Asset), request.AssetId.Value) : null;
        var material = new LearningMaterial
        {
            TenantId = current.TenantId, BatchId = request.BatchId, TopicId = request.TopicId,
            Title = request.Title, Type = request.Type, StorageKey = asset?.StorageKey,
            ExternalUrl = request.ExternalUrl, DurationMinutes = request.DurationMinutes,
            Order = request.Order, AuthorId = current.UserId
        };
        db.Set<LearningMaterial>().Add(material);
        await db.SaveChangesAsync(ct);
        return material.Id;
    }
}

public sealed record ListLearningMaterials(Guid? BatchId, Guid? TopicId)
    : IRequest<IReadOnlyList<LearningMaterialDto>>, IRequiresTenant;

public sealed record LearningMaterialDto(Guid Id, string Title, MaterialType Type,
    string? StorageKey, string? ExternalUrl, int? DurationMinutes, bool IsPublished);

public sealed class ListLearningMaterialsHandler(IApplicationDbContext db)
    : IRequestHandler<ListLearningMaterials, IReadOnlyList<LearningMaterialDto>>
{
    public async Task<IReadOnlyList<LearningMaterialDto>> Handle(ListLearningMaterials request, CancellationToken ct)
    {
        var query = db.Set<LearningMaterial>().AsNoTracking().AsQueryable();
        if (request.BatchId.HasValue) query = query.Where(m => m.BatchId == request.BatchId.Value);
        if (request.TopicId.HasValue) query = query.Where(m => m.TopicId == request.TopicId.Value);
        return await query.OrderBy(m => m.Order)
            .Select(m => new LearningMaterialDto(m.Id, m.Title, m.Type, m.StorageKey,
                m.ExternalUrl, m.DurationMinutes, m.IsPublished)).ToListAsync(ct);
    }
}

public sealed record PublishLearningMaterial(Guid MaterialId, bool Publish)
    : IRequest<bool>, IRequiresTenant;

public sealed class PublishLearningMaterialHandler(IApplicationDbContext db)
    : IRequestHandler<PublishLearningMaterial, bool>
{
    public async Task<bool> Handle(PublishLearningMaterial request, CancellationToken ct)
    {
        var material = await db.Set<LearningMaterial>().FirstOrDefaultAsync(m => m.Id == request.MaterialId, ct)
                       ?? throw new NotFoundException(nameof(LearningMaterial), request.MaterialId);
        material.IsPublished = request.Publish;
        material.PublishedAt = request.Publish ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
