using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;

namespace CoachingPlatform.Domain.Crm;

/// <summary>Admissions funnel entry. Converts into an AdmissionApplication then a User + Enrollment.</summary>
public sealed class Lead : TenantEntity
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Website;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public Guid? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? LastContactedAt { get; set; }
    public Guid? ConvertedUserId { get; set; }
    public Guid? CourseId { get; set; }
    public ICollection<FollowUp> FollowUps { get; set; } = [];
}

public sealed class AdmissionApplication : TenantEntity
{
    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public Guid? AcademicYearId { get; set; }
    public string ApplicantFirstName { get; set; } = default!;
    public string ApplicantLastName { get; set; } = default!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset? DecisionAt { get; set; }
    public Guid? DecisionById { get; set; }
    public string? DecisionNotes { get; set; }
    public decimal? EntranceTestScore { get; set; }
    public Guid? ConvertedUserId { get; set; }
    public Guid? EnrollmentId { get; set; }

    public ICollection<ApplicationDocument> Documents { get; set; } = [];
}

public sealed class ApplicationDocument : TenantEntity
{
    public Guid ApplicationId { get; set; }
    public AdmissionApplication Application { get; set; } = default!;
    public string DocumentType { get; set; } = default!;   // NID, Transcript, Photo...
    public string FileName { get; set; } = default!;
    public string StorageKey { get; set; } = default!;
    public bool IsVerified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
}

public sealed class FollowUp : TenantEntity
{
    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public Guid? ApplicationId { get; set; }
    public AdmissionApplication? Application { get; set; }
    public string Note { get; set; } = default!;
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid OwnerId { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Scheduled;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
}
