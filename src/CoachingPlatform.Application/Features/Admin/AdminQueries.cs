using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Content;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Admin;

public sealed record GetAuditLogs(string? EntityName, string? EntityId, int Page, int PageSize)
    : IRequest<PagedResult<AuditLogDto>>, IRequiresTenant;

public sealed record AuditLogDto(Guid Id, string EntityName, string EntityId, string Action,
    string? ActorEmail, string? ChangesJson, DateTimeOffset OccurredAt);

public sealed class GetAuditLogsHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<GetAuditLogs, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogs request, CancellationToken ct)
    {
        var query = db.Set<AuditLog>().AsNoTracking()
            .Where(a => a.TenantId == current.TenantId);
        if (!string.IsNullOrWhiteSpace(request.EntityName)) query = query.Where(a => a.EntityName == request.EntityName);
        if (!string.IsNullOrWhiteSpace(request.EntityId)) query = query.Where(a => a.EntityId == request.EntityId);
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * size).Take(size)
            .Select(a => new AuditLogDto(a.Id, a.EntityName, a.EntityId, a.Action,
                a.ActorEmail, a.ChangesJson, a.OccurredAt)).ToListAsync(ct);
        return new PagedResult<AuditLogDto>(items, total, page, size);
    }
}

public sealed record IsFeatureEnabled(string Key) : IRequest<bool>, IRequiresTenant;

public sealed class IsFeatureEnabledHandler(IApplicationDbContext db)
    : IRequestHandler<IsFeatureEnabled, bool>
{
    public async Task<bool> Handle(IsFeatureEnabled request, CancellationToken ct)
        => await db.Set<FeatureFlag>().AnyAsync(f => f.Key == request.Key && f.IsEnabled, ct);
}

public sealed record SetFeatureFlag(string Key, string? Description, bool IsEnabled, string? TargetingJson)
    : IRequest<Guid>, IRequiresTenant;

public sealed class SetFeatureFlagHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<SetFeatureFlag, Guid>
{
    public async Task<Guid> Handle(SetFeatureFlag request, CancellationToken ct)
    {
        var flag = await db.Set<FeatureFlag>().FirstOrDefaultAsync(f => f.Key == request.Key, ct);
        if (flag is null)
        {
            flag = new FeatureFlag { TenantId = current.TenantId, Key = request.Key };
            db.Set<FeatureFlag>().Add(flag);
        }
        flag.Description = request.Description;
        flag.IsEnabled = request.IsEnabled;
        flag.TargetingJson = request.TargetingJson;
        await db.SaveChangesAsync(ct);
        return flag.Id;
    }
}

public sealed record Search(string Term, int Page, int PageSize) : IRequest<SearchResultDto>, IRequiresTenant;

public sealed record SearchResultDto(
    IReadOnlyList<SearchItemDto> Students, IReadOnlyList<SearchItemDto> Courses,
    IReadOnlyList<SearchItemDto> Batches, IReadOnlyList<SearchItemDto> Assets);

public sealed record SearchItemDto(Guid Id, string Name);

public sealed class SearchHandler(IApplicationDbContext db) : IRequestHandler<Search, SearchResultDto>
{
    public async Task<SearchResultDto> Handle(Search request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Term))
            return new SearchResultDto([], [], [], []);
        var term = request.Term.Trim();
        var take = Math.Clamp(request.PageSize, 1, 50);
        var students = await db.Set<Domain.Identity.User>()
            .Where(u => u.FirstName.Contains(term) || u.LastName.Contains(term) || u.Email.Contains(term))
            .Take(take).Select(u => new SearchItemDto(u.Id, u.FullName)).ToListAsync(ct);
        var courses = await db.Set<Domain.Academics.Course>()
            .Where(c => c.Name.Contains(term) || c.Code.Contains(term))
            .Take(take).Select(c => new SearchItemDto(c.Id, c.Name)).ToListAsync(ct);
        var batches = await db.Set<Domain.Academics.Batch>()
            .Where(b => b.Name.Contains(term) || b.Code.Contains(term))
            .Take(take).Select(b => new SearchItemDto(b.Id, b.Name)).ToListAsync(ct);
        var assets = await db.Set<Asset>()
            .Where(a => a.FileName.Contains(term))
            .Take(take).Select(a => new SearchItemDto(a.Id, a.FileName)).ToListAsync(ct);
        return new SearchResultDto(students, courses, batches, assets);
    }
}

public sealed record ExportStudentData(Guid StudentId) : IRequest<string>, IRequiresTenant;

public sealed class ExportStudentDataHandler(IApplicationDbContext db) : IRequestHandler<ExportStudentData, string>
{
    public async Task<string> Handle(ExportStudentData request, CancellationToken ct)
    {
        var student = await db.Set<Domain.Identity.User>().FirstOrDefaultAsync(u => u.Id == request.StudentId, ct)
                      ?? throw new NotFoundException(nameof(Domain.Identity.User), request.StudentId);
        var enrollments = await db.Set<Domain.Academics.Enrollment>().Where(e => e.StudentId == request.StudentId)
            .Select(e => new { e.Id, e.BatchId, e.Status, e.RollNumber }).ToListAsync(ct);
        var grades = await db.Set<Domain.Assessment.GradeEntry>().Where(g => g.StudentId == request.StudentId)
            .Select(g => new { g.Id, g.Title, g.MarksObtained, g.MaxMarks }).ToListAsync(ct);
        var invoices = await db.Set<Domain.Finance.Invoice>().Where(i => i.StudentId == request.StudentId)
            .Select(i => new { i.Id, i.InvoiceNumber, Total = i.Total.Amount }).ToListAsync(ct);
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            student = new { student.Id, student.Email, student.FirstName, student.LastName },
            enrollments, grades, invoices
        });
    }
}

public sealed record EraseStudentData(Guid StudentId) : IRequest<bool>, IRequiresTenant;

public sealed class EraseStudentDataHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<EraseStudentData, bool>
{
    public async Task<bool> Handle(EraseStudentData request, CancellationToken ct)
    {
        var student = await db.Set<Domain.Identity.User>().FirstOrDefaultAsync(u => u.Id == request.StudentId, ct)
                      ?? throw new NotFoundException(nameof(Domain.Identity.User), request.StudentId);
        var anonymized = $"erased-{student.Id:N}@invalid.local";
        student.Email = anonymized;
        student.FirstName = "Erased";
        student.LastName = "User";
        student.Status = UserStatus.Archived;
        student.IsDeleted = true;
        student.DeletedAt = DateTimeOffset.UtcNow;
        student.DeletedBy = current.UserId;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
