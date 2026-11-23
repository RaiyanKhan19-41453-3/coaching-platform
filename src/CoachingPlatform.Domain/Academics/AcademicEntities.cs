using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Tenancy;

namespace CoachingPlatform.Domain.Academics;

public sealed class AcademicYear : TenantEntity
{
    public string Name { get; set; } = default!;          // "2026-27"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Term> Terms { get; set; } = [];
}

public sealed class Term : TenantEntity
{
    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Order { get; set; }
}

public sealed class Course : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public int DurationMonths { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Subject> Subjects { get; set; } = [];
}

public sealed class Subject : TenantEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Topic> Topics { get; set; } = [];
}

/// <summary>Self-referencing syllabus tree (Subject &gt; Topic &gt; Sub-topic).</summary>
public sealed class Topic : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;
    public Guid? ParentTopicId { get; set; }
    public Topic? ParentTopic { get; set; }
    public string Title { get; set; } = default!;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
    public ICollection<Topic> Children { get; set; } = [];
}

public sealed class Batch : TenantEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;
    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = default!;
    public Guid? TermId { get; set; }
    public Term? Term { get; set; }

    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Capacity { get; set; } = 30;
    public BatchStatus Status { get; set; } = BatchStatus.Draft;
    public DateTimeOffset? EnrollmentOpenAt { get; set; }
    public DateTimeOffset? EnrollmentCloseAt { get; set; }
    public int SyllabusVersion { get; set; } = 1;
    public string? Notes { get; set; }

    public ICollection<BatchTeacher> Teachers { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<BatchTopic> Syllabus { get; set; } = [];
}

public sealed class BatchTeacher : TenantEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = default!;
    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = default!;
    public bool IsLead { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}

public sealed class Enrollment : TenantEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = default!;
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public string? RollNumber { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Applied;
    public DateTimeOffset EnrolledAt { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public Guid? TransferredFromEnrollmentId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Topic coverage planned for a particular batch (tracks syllabus progress).</summary>
public sealed class BatchTopic : TenantEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = default!;
    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = default!;
    public int Order { get; set; }
    public DateOnly? PlannedDate { get; set; }
    public DateOnly? CoveredDate { get; set; }
}

public sealed class LearningMaterial : TenantEntity
{
    public Guid? TopicId { get; set; }
    public Topic? Topic { get; set; }
    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public string Title { get; set; } = default!;
    public MaterialType Type { get; set; } = MaterialType.Document;
    public string? StorageKey { get; set; }
    public string? ExternalUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int Order { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? AuthorId { get; set; }
}

public enum MaterialType { Document, Pdf, Video, Slide, Audio, Link, Code }
