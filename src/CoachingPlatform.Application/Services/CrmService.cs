using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Application.DTOs.Crm;
using CoachingPlatform.Application.Features.Crm;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface ICrmService
{
    Task<Guid> CreateLeadAsync(CreateLeadRequest r, CancellationToken ct = default);
    Task<bool> UpdateLeadStatusAsync(Guid id, UpdateLeadStatusRequest r, CancellationToken ct = default);
    Task<Guid> CreateApplicationAsync(CreateApplicationRequest r, CancellationToken ct = default);
    Task<Guid> AddDocumentAsync(Guid applicationId, AddApplicationDocumentRequest r, CancellationToken ct = default);
    Task<bool> ReviewApplicationAsync(Guid id, ReviewApplicationRequest r, CancellationToken ct = default);
    Task<Guid> EnrollApplicantAsync(Guid id, EnrollApplicantRequest r, CancellationToken ct = default);
    Task<Guid> CreateFollowUpAsync(CreateFollowUpRequest r, CancellationToken ct = default);
    Task<bool> CompleteFollowUpAsync(Guid id, string? note, CancellationToken ct = default);
    Task<IReadOnlyList<LeadResponse>> ListLeadsAsync(LeadStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationResponse>> ListApplicationsAsync(Guid? courseId, ApplicationStatus? status, CancellationToken ct = default);
}

public sealed class CrmService(ISender sender) : ICrmService
{
    public Task<Guid> CreateLeadAsync(CreateLeadRequest r, CancellationToken ct)
        => sender.Send(new CreateLead(r.FirstName, r.LastName, r.Email, r.PhoneNumber, r.Source,
            r.AssignedToId, r.CourseId, r.Notes), ct);
    public Task<bool> UpdateLeadStatusAsync(Guid id, UpdateLeadStatusRequest r, CancellationToken ct)
        => sender.Send(new UpdateLeadStatus(id, r.Status, r.Notes), ct);
    public Task<Guid> CreateApplicationAsync(CreateApplicationRequest r, CancellationToken ct)
        => sender.Send(new CreateApplication(r.LeadId, r.CourseId, r.AcademicYearId, r.FirstName,
            r.LastName, r.Email, r.PhoneNumber, r.GuardianName, r.GuardianPhone), ct);
    public Task<Guid> AddDocumentAsync(Guid id, AddApplicationDocumentRequest r, CancellationToken ct)
        => sender.Send(new AddApplicationDocument(id, r.DocumentType, r.FileName, r.StorageKey), ct);
    public Task<bool> ReviewApplicationAsync(Guid id, ReviewApplicationRequest r, CancellationToken ct)
        => sender.Send(new ReviewApplication(id, r.Status, r.Notes, r.TestScore), ct);
    public Task<Guid> EnrollApplicantAsync(Guid id, EnrollApplicantRequest r, CancellationToken ct)
        => sender.Send(new EnrollApplicant(id, r.BatchId, r.Password, r.GuardianUserId), ct);
    public Task<Guid> CreateFollowUpAsync(CreateFollowUpRequest r, CancellationToken ct)
        => sender.Send(new CreateFollowUp(r.LeadId, r.ApplicationId, r.Note, r.ScheduledAt), ct);
    public Task<bool> CompleteFollowUpAsync(Guid id, string? note, CancellationToken ct)
        => sender.Send(new CompleteFollowUp(id, note), ct);
    public async Task<IReadOnlyList<LeadResponse>> ListLeadsAsync(LeadStatus? status, CancellationToken ct)
    {
        var rows = await sender.Send(new ListLeads(status), ct);
        return rows.Select(x => new LeadResponse(x.Id, x.Name, x.Email, x.Status, x.CreatedAt)).ToList();
    }
    public async Task<IReadOnlyList<ApplicationResponse>> ListApplicationsAsync(Guid? courseId, ApplicationStatus? status, CancellationToken ct)
    {
        var rows = await sender.Send(new ListApplications(courseId, status), ct);
        return rows.Select(x => new ApplicationResponse(x.Id, x.Name, x.CourseId, x.Status, x.AppliedAt)).ToList();
    }
}
