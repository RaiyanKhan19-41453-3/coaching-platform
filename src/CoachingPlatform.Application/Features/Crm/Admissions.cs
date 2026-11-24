using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Crm;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Domain.Communication;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoachingPlatform.Application.Features.Crm;

public sealed record CreateLead(string FirstName, string LastName, string? Email, string? PhoneNumber,
    LeadSource Source, Guid? AssignedToId, Guid? CourseId, string? Notes) : IRequest<Guid>, IRequiresTenant;
public sealed class CreateLeadValidator : AbstractValidator<CreateLead>
{
    public CreateLeadValidator() { RuleFor(x => x.FirstName).NotEmpty(); RuleFor(x => x.LastName).NotEmpty(); }
}
public sealed class CreateLeadHandler(IApplicationDbContext db, ICurrentUser current) : IRequestHandler<CreateLead, Guid>
{
    public async Task<Guid> Handle(CreateLead r, CancellationToken ct)
    {
        var lead = new Lead
        {
            TenantId = current.TenantId, FirstName = r.FirstName, LastName = r.LastName,
            Email = r.Email, PhoneNumber = r.PhoneNumber, Source = r.Source,
            AssignedToId = r.AssignedToId, CourseId = r.CourseId, Notes = r.Notes
        };
        db.Set<Lead>().Add(lead);
        await db.SaveChangesAsync(ct);
        return lead.Id;
    }
}

public sealed record UpdateLeadStatus(Guid LeadId, LeadStatus Status, string? Notes) : IRequest<bool>, IRequiresTenant;
public sealed class UpdateLeadStatusHandler(IApplicationDbContext db, ICurrentUser current) : IRequestHandler<UpdateLeadStatus, bool>
{
    public async Task<bool> Handle(UpdateLeadStatus r, CancellationToken ct)
    {
        var lead = await db.Set<Lead>().FirstOrDefaultAsync(x => x.Id == r.LeadId, ct)
                   ?? throw new NotFoundException(nameof(Lead), r.LeadId);
        lead.Status = r.Status; lead.Notes = r.Notes ?? lead.Notes; lead.LastContactedAt = DateTimeOffset.UtcNow;
        if (r.Status == LeadStatus.Converted && lead.ConvertedUserId.HasValue)
        {
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                TenantId = current.TenantId, EventType = "crm.lead_converted",
                PayloadJson = JsonSerializer.Serialize(new { lead.Id, lead.ConvertedUserId }),
                OccurredAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record CreateApplication(Guid? LeadId, Guid CourseId, Guid? AcademicYearId, string FirstName,
    string LastName, string? Email, string? PhoneNumber, string? GuardianName, string? GuardianPhone)
    : IRequest<Guid>, IRequiresTenant;
public sealed class CreateApplicationValidator : AbstractValidator<CreateApplication>
{
    public CreateApplicationValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty(); RuleFor(x => x.FirstName).NotEmpty(); RuleFor(x => x.LastName).NotEmpty();
    }
}
public sealed class CreateApplicationHandler(IApplicationDbContext db, ICurrentUser current) : IRequestHandler<CreateApplication, Guid>
{
    public async Task<Guid> Handle(CreateApplication r, CancellationToken ct)
    {
        if (!await db.Set<Course>().AnyAsync(c => c.Id == r.CourseId, ct))
            throw new NotFoundException(nameof(Course), r.CourseId);
        var application = new AdmissionApplication
        {
            TenantId = current.TenantId, LeadId = r.LeadId, CourseId = r.CourseId,
            AcademicYearId = r.AcademicYearId, ApplicantFirstName = r.FirstName,
            ApplicantLastName = r.LastName, Email = r.Email, PhoneNumber = r.PhoneNumber,
            GuardianName = r.GuardianName, GuardianPhone = r.GuardianPhone,
            Status = ApplicationStatus.Submitted, AppliedAt = DateTimeOffset.UtcNow
        };
        db.Set<AdmissionApplication>().Add(application);
        await db.SaveChangesAsync(ct);
        return application.Id;
    }
}

public sealed record AddApplicationDocument(Guid ApplicationId, string DocumentType, string FileName, string StorageKey)
    : IRequest<Guid>, IRequiresTenant;
public sealed class AddApplicationDocumentHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<AddApplicationDocument, Guid>
{
    public async Task<Guid> Handle(AddApplicationDocument r, CancellationToken ct)
    {
        if (!await db.Set<AdmissionApplication>().AnyAsync(a => a.Id == r.ApplicationId, ct))
            throw new NotFoundException(nameof(AdmissionApplication), r.ApplicationId);
        var doc = new ApplicationDocument
        {
            TenantId = current.TenantId, ApplicationId = r.ApplicationId,
            DocumentType = r.DocumentType, FileName = r.FileName, StorageKey = r.StorageKey
        };
        db.Set<ApplicationDocument>().Add(doc);
        await db.SaveChangesAsync(ct);
        return doc.Id;
    }
}

public sealed record ReviewApplication(Guid ApplicationId, ApplicationStatus Status, string? Notes, decimal? TestScore)
    : IRequest<bool>, IRequiresTenant;
public sealed class ReviewApplicationHandler(IApplicationDbContext db, ICurrentUser current) : IRequestHandler<ReviewApplication, bool>
{
    public async Task<bool> Handle(ReviewApplication r, CancellationToken ct)
    {
        var application = await db.Set<AdmissionApplication>().FirstOrDefaultAsync(a => a.Id == r.ApplicationId, ct)
                          ?? throw new NotFoundException(nameof(AdmissionApplication), r.ApplicationId);
        if (application.Status is ApplicationStatus.Enrolled or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn)
            throw new ConflictException("Application has already been decided.");
        application.Status = r.Status;
        application.DecisionAt = DateTimeOffset.UtcNow;
        application.DecisionById = current.UserId;
        application.DecisionNotes = r.Notes;
        if (r.TestScore.HasValue) application.EntranceTestScore = r.TestScore;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>Converts an accepted application into a User + Enrollment + guardian link in one transaction.</summary>
public sealed record EnrollApplicant(Guid ApplicationId, Guid BatchId, string? Password, Guid? GuardianUserId)
    : IRequest<Guid>, IRequiresTenant;
public sealed class EnrollApplicantHandler(IApplicationDbContext db, ICurrentUser current, IPasswordHasher hasher)
    : IRequestHandler<EnrollApplicant, Guid>
{
    public async Task<Guid> Handle(EnrollApplicant r, CancellationToken ct)
    {
        var application = await db.Set<AdmissionApplication>().FirstOrDefaultAsync(a => a.Id == r.ApplicationId, ct)
                          ?? throw new NotFoundException(nameof(AdmissionApplication), r.ApplicationId);
        if (application.Status != ApplicationStatus.Accepted && application.Status != ApplicationStatus.Offered)
            throw new ConflictException("Only accepted/offered applications can be enrolled.");
        var batch = await db.Set<Batch>().FirstOrDefaultAsync(b => b.Id == r.BatchId, ct)
                    ?? throw new NotFoundException(nameof(Batch), r.BatchId);
        var active = await db.Set<Enrollment>().CountAsync(e => e.BatchId == batch.Id &&
            (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed), ct);
        if (active >= batch.Capacity) throw new ConflictException("Batch is full.");

        var email = application.Email ?? $"student-{Guid.NewGuid():N}@enrolled.local";
        if (await db.Set<User>().AnyAsync(u => u.Email == email, ct)) email = $"{Guid.NewGuid():N}-{email}";
        var user = new User
        {
            Email = email, EmailConfirmed = string.IsNullOrWhiteSpace(r.Password),
            PhoneNumber = application.PhoneNumber, PhoneNumberConfirmed = false,
            FirstName = application.ApplicantFirstName, LastName = application.ApplicantLastName,
            PasswordHash = string.IsNullOrWhiteSpace(r.Password) ? null : hasher.Hash(r.Password),
            Status = UserStatus.Active
        };
        db.Set<User>().Add(user);
        db.Set<TenantMembership>().Add(new TenantMembership
        {
            TenantId = current.TenantId, UserId = user.Id, Status = MembershipStatus.Active,
            JoinedAt = DateTimeOffset.UtcNow
        });
        var studentRole = await db.Set<Role>().FirstOrDefaultAsync(x => x.Name == "Student", ct);
        if (studentRole is not null)
            db.Set<UserRole>().Add(new UserRole
            {
                TenantId = current.TenantId, UserId = user.Id, RoleId = studentRole.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
        if (r.GuardianUserId.HasValue)
            db.Set<StudentGuardian>().Add(new StudentGuardian
            {
                TenantId = current.TenantId, StudentId = user.Id, GuardianId = r.GuardianUserId.Value
            });
        var enrollment = new Enrollment
        {
            TenantId = current.TenantId, BatchId = batch.Id, StudentId = user.Id,
            Status = EnrollmentStatus.Enrolled, EnrolledAt = DateTimeOffset.UtcNow,
            RollNumber = $"{batch.Code}-{active + 1:000}"
        };
        db.Set<Enrollment>().Add(enrollment);
        db.Set<Batch>().Entry(batch).Property(b => b.SyllabusVersion).IsModified = true;
        application.Status = ApplicationStatus.Enrolled;
        application.ConvertedUserId = user.Id;
        application.EnrollmentId = enrollment.Id;
        if (application.LeadId.HasValue)
        {
            var lead = await db.Set<Lead>().FirstOrDefaultAsync(l => l.Id == application.LeadId.Value, ct);
            if (lead is not null) { lead.Status = LeadStatus.Converted; lead.ConvertedUserId = user.Id; }
        }
        await db.SaveChangesAsync(ct);
        return user.Id;
    }
}

public sealed record CreateFollowUp(Guid? LeadId, Guid? ApplicationId, string Note, DateTimeOffset ScheduledAt)
    : IRequest<Guid>, IRequiresTenant;
public sealed class CreateFollowUpHandler(IApplicationDbContext db, ICurrentUser current) : IRequestHandler<CreateFollowUp, Guid>
{
    public async Task<Guid> Handle(CreateFollowUp r, CancellationToken ct)
    {
        var follow = new FollowUp
        {
            TenantId = current.TenantId, LeadId = r.LeadId, ApplicationId = r.ApplicationId,
            Note = r.Note, ScheduledAt = r.ScheduledAt, OwnerId = current.UserId ?? Guid.Empty
        };
        db.Set<FollowUp>().Add(follow);
        await db.SaveChangesAsync(ct);
        return follow.Id;
    }
}

public sealed record CompleteFollowUp(Guid FollowUpId, string? Note) : IRequest<bool>, IRequiresTenant;
public sealed class CompleteFollowUpHandler(IApplicationDbContext db) : IRequestHandler<CompleteFollowUp, bool>
{
    public async Task<bool> Handle(CompleteFollowUp r, CancellationToken ct)
    {
        var follow = await db.Set<FollowUp>().FirstOrDefaultAsync(f => f.Id == r.FollowUpId, ct)
                     ?? throw new NotFoundException(nameof(FollowUp), r.FollowUpId);
        follow.Status = FollowUpStatus.Completed;
        follow.CompletedAt = DateTimeOffset.UtcNow;
        if (r.Note is not null) follow.Note = r.Note;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record ListLeads(LeadStatus? Status) : IRequest<IReadOnlyList<LeadDto>>, IRequiresTenant;
public sealed record LeadDto(Guid Id, string Name, string? Email, LeadStatus Status, DateTimeOffset CreatedAt);
public sealed class ListLeadsHandler(IApplicationDbContext db) : IRequestHandler<ListLeads, IReadOnlyList<LeadDto>>
{
    public async Task<IReadOnlyList<LeadDto>> Handle(ListLeads r, CancellationToken ct)
    {
        var q = db.Set<Lead>().AsNoTracking().AsQueryable();
        if (r.Status.HasValue) q = q.Where(x => x.Status == r.Status.Value);
        return await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadDto(x.Id, x.FirstName + " " + x.LastName, x.Email, x.Status, x.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed record ListApplications(Guid? CourseId, ApplicationStatus? Status)
    : IRequest<IReadOnlyList<ApplicationDto>>, IRequiresTenant;
public sealed record ApplicationDto(Guid Id, string Name, Guid CourseId, ApplicationStatus Status, DateTimeOffset AppliedAt);
public sealed class ListApplicationsHandler(IApplicationDbContext db) : IRequestHandler<ListApplications, IReadOnlyList<ApplicationDto>>
{
    public async Task<IReadOnlyList<ApplicationDto>> Handle(ListApplications r, CancellationToken ct)
    {
        var q = db.Set<AdmissionApplication>().AsNoTracking().AsQueryable();
        if (r.CourseId.HasValue) q = q.Where(x => x.CourseId == r.CourseId.Value);
        if (r.Status.HasValue) q = q.Where(x => x.Status == r.Status.Value);
        return await q.OrderByDescending(x => x.AppliedAt)
            .Select(x => new ApplicationDto(x.Id, x.ApplicantFirstName + " " + x.ApplicantLastName,
                x.CourseId, x.Status, x.AppliedAt)).ToListAsync(ct);
    }
}
