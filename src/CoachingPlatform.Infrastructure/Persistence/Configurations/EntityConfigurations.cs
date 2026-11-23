using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Domain.Content;
using CoachingPlatform.Domain.Crm;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Domain.Reporting;
using CoachingPlatform.Domain.Scheduling;
using CoachingPlatform.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachingPlatform.Infrastructure.Persistence.Configurations;

/// <summary>Shared mapping for tenant rows: hard keys, soft delete, xmin concurrency.</summary>
public abstract class TenantEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : TenantEntity
{
    public virtual void Configure(EntityTypeBuilder<T> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.TenantId).IsRequired();
        b.Property(e => e.CreatedAt).IsRequired();
        b.Property(e => e.IsDeleted).HasDefaultValue(false);
        b.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
    }
}

public abstract class GlobalEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : GlobalEntity
{
    public virtual void Configure(EntityTypeBuilder<T> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.CreatedAt).IsRequired();
        b.Property(e => e.IsDeleted).HasDefaultValue(false);
    }
}


public sealed class TenantSettingConfiguration : TenantEntityConfiguration<TenantSetting>
{
    public override void Configure(EntityTypeBuilder<TenantSetting> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Category, e.Key }).IsUnique();
    }
}

public sealed class FeatureFlagConfiguration : TenantEntityConfiguration<FeatureFlag>
{
    public override void Configure(EntityTypeBuilder<FeatureFlag> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
    }
}

public sealed class TenantMembershipConfiguration : TenantEntityConfiguration<TenantMembership>
{
    public override void Configure(EntityTypeBuilder<TenantMembership> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class RoleConfiguration : TenantEntityConfiguration<Role>
{
    public override void Configure(EntityTypeBuilder<Role> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
    }
}

public sealed class RolePermissionConfiguration : TenantEntityConfiguration<RolePermission>
{
    public override void Configure(EntityTypeBuilder<RolePermission> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class UserRoleConfiguration : TenantEntityConfiguration<UserRole>
{
    public override void Configure(EntityTypeBuilder<UserRole> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class StudentGuardianConfiguration : TenantEntityConfiguration<StudentGuardian>
{
    public override void Configure(EntityTypeBuilder<StudentGuardian> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.StudentId, e.GuardianId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class AcademicYearConfiguration : TenantEntityConfiguration<AcademicYear>
{
    public override void Configure(EntityTypeBuilder<AcademicYear> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class TermConfiguration : TenantEntityConfiguration<Term>
{
    public override void Configure(EntityTypeBuilder<Term> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.AcademicYearId, e.Name }).IsUnique();
    }
}

public sealed class CourseConfiguration : TenantEntityConfiguration<Course>
{
    public override void Configure(EntityTypeBuilder<Course> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Code }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasIndex(e => new { e.TenantId, e.Name });
        b.Property(e => e.Code).HasMaxLength(50).IsRequired();
    }
}

public sealed class SubjectConfiguration : TenantEntityConfiguration<Subject>
{
    public override void Configure(EntityTypeBuilder<Subject> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.CourseId, e.Code }).IsUnique();
    }
}

public sealed class TopicConfiguration : TenantEntityConfiguration<Topic>
{
    public override void Configure(EntityTypeBuilder<Topic> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.SubjectId, e.ParentTopicId, e.Order });
    }
}

public sealed class BatchConfiguration : TenantEntityConfiguration<Batch>
{
    public override void Configure(EntityTypeBuilder<Batch> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Code }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasIndex(e => new { e.CourseId, e.Status });
        b.Property(e => e.Code).HasMaxLength(50).IsRequired();
    }
}

public sealed class BatchTeacherConfiguration : TenantEntityConfiguration<BatchTeacher>
{
    public override void Configure(EntityTypeBuilder<BatchTeacher> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.BatchId, e.TeacherId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class EnrollmentConfiguration : TenantEntityConfiguration<Enrollment>
{
    public override void Configure(EntityTypeBuilder<Enrollment> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.BatchId, e.StudentId }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasIndex(e => new { e.StudentId, e.Status });
    }
}

public sealed class BatchTopicConfiguration : TenantEntityConfiguration<BatchTopic>
{
    public override void Configure(EntityTypeBuilder<BatchTopic> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.BatchId, e.TopicId }).IsUnique();
    }
}

public sealed class LearningMaterialConfiguration : TenantEntityConfiguration<LearningMaterial>
{
    public override void Configure(EntityTypeBuilder<LearningMaterial> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.TopicId });
    }
}

public sealed class RoomConfiguration : TenantEntityConfiguration<Room>
{
    public override void Configure(EntityTypeBuilder<Room> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Code });
    }
}

public sealed class ScheduleRuleConfiguration : TenantEntityConfiguration<ScheduleRule>
{
    public override void Configure(EntityTypeBuilder<ScheduleRule> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.BatchId, e.DayOfWeek, e.StartTime });
    }
}

public sealed class ClassSessionConfiguration : TenantEntityConfiguration<ClassSession>
{
    public override void Configure(EntityTypeBuilder<ClassSession> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.StartUtc });
        b.HasIndex(e => new { e.BatchId, e.StartUtc });
        b.HasIndex(e => new { e.ScheduleRuleId, e.StartUtc })
         .IsUnique().HasFilter("\"ScheduleRuleId\" IS NOT NULL AND \"IsDeleted\" = false");
    }
}

public sealed class AttendanceConfiguration : TenantEntityConfiguration<Attendance>
{
    public override void Configure(EntityTypeBuilder<Attendance> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.SessionId, e.StudentId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class LeaveRequestConfiguration : TenantEntityConfiguration<LeaveRequest>
{
    public override void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.StudentId, e.Status });
    }
}

public sealed class QuestionConfiguration : TenantEntityConfiguration<Question>
{
    public override void Configure(EntityTypeBuilder<Question> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.SubjectId });
        b.HasIndex(e => e.TopicId);
    }
}

public sealed class AssessmentConfiguration : TenantEntityConfiguration<Assessment>
{
    public override void Configure(EntityTypeBuilder<Assessment> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.SubjectId });
        b.HasIndex(e => e.BatchId);
    }
}

public sealed class AssessmentQuestionConfiguration : TenantEntityConfiguration<AssessmentQuestion>
{
    public override void Configure(EntityTypeBuilder<AssessmentQuestion> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.AssessmentId, e.Order }).IsUnique();
    }
}

public sealed class AssessmentAttemptConfiguration : TenantEntityConfiguration<AssessmentAttempt>
{
    public override void Configure(EntityTypeBuilder<AssessmentAttempt> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.AssessmentId, e.StudentId, e.AttemptNumber }).IsUnique();
    }
}

public sealed class StudentAnswerConfiguration : TenantEntityConfiguration<StudentAnswer>
{
    public override void Configure(EntityTypeBuilder<StudentAnswer> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.AttemptId, e.QuestionId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class GradeEntryConfiguration : TenantEntityConfiguration<GradeEntry>
{
    public override void Configure(EntityTypeBuilder<GradeEntry> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.StudentId, e.BatchId });
    }
}

public sealed class RubricConfiguration : TenantEntityConfiguration<Rubric>
{
    public override void Configure(EntityTypeBuilder<Rubric> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.AssessmentId });
    }
}

public sealed class RubricCriterionConfiguration : TenantEntityConfiguration<RubricCriterion>
{
    public override void Configure(EntityTypeBuilder<RubricCriterion> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.RubricId, e.Order });
    }
}

public sealed class FeeStructureConfiguration : TenantEntityConfiguration<FeeStructure>
{
    public override void Configure(EntityTypeBuilder<FeeStructure> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Code });
    }
}

public sealed class InvoiceConfiguration : TenantEntityConfiguration<Invoice>
{
    public override void Configure(EntityTypeBuilder<Invoice> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.InvoiceNumber }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasIndex(e => new { e.StudentId, e.Status });
        b.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
    }
}

public sealed class InvoiceLineConfiguration : TenantEntityConfiguration<InvoiceLine>
{
    public override void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.InvoiceId, e.Order });
    }
}

public sealed class PaymentConfiguration : TenantEntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> b)
    {
        base.Configure(b);
        b.HasIndex(e => e.GatewayTransactionId);
        b.HasIndex(e => new { e.StudentId, e.PaidAt });
        b.HasIndex(e => new { e.Gateway, e.GatewayTransactionId })
         .IsUnique().HasFilter("\"GatewayTransactionId\" IS NOT NULL AND \"IsDeleted\" = false");
    }
}

public sealed class DiscountConfiguration : TenantEntityConfiguration<Discount>
{
    public override void Configure(EntityTypeBuilder<Discount> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Code }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class ScholarshipConfiguration : TenantEntityConfiguration<Scholarship>
{
    public override void Configure(EntityTypeBuilder<Scholarship> b)
    {
        base.Configure(b);
        b.HasIndex(e => e.StudentId);
    }
}

public sealed class LedgerEntryConfiguration : TenantEntityConfiguration<LedgerEntry>
{
    public override void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.PostedAt });
        b.HasIndex(e => new { e.SourceType, e.SourceId });
    }
}

public sealed class ExpenseConfiguration : TenantEntityConfiguration<Expense>
{
    public override void Configure(EntityTypeBuilder<Expense> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.IncidentAt });
    }
}

public sealed class RefundConfiguration : TenantEntityConfiguration<Refund>
{
    public override void Configure(EntityTypeBuilder<Refund> b)
    {
        base.Configure(b);
        b.HasIndex(e => e.PaymentId);
    }
}

public sealed class AnnouncementConfiguration : TenantEntityConfiguration<Announcement>
{
    public override void Configure(EntityTypeBuilder<Announcement> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.PublishedAt });
    }
}

public sealed class NotificationConfiguration : TenantEntityConfiguration<Notification>
{
    public override void Configure(EntityTypeBuilder<Notification> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.UserId, e.IsRead });
        b.HasIndex(e => new { e.TenantId, e.Type });
    }
}

public sealed class NotificationPreferenceConfiguration : TenantEntityConfiguration<NotificationPreference>
{
    public override void Configure(EntityTypeBuilder<NotificationPreference> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.UserId, e.Channel, e.Type }).IsUnique();
    }
}

public sealed class ConversationConfiguration : TenantEntityConfiguration<Conversation>
{
    public override void Configure(EntityTypeBuilder<Conversation> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.LastMessageAt });
    }
}

public sealed class ConversationParticipantConfiguration : TenantEntityConfiguration<ConversationParticipant>
{
    public override void Configure(EntityTypeBuilder<ConversationParticipant> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.ConversationId, e.UserId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class MessageConfiguration : TenantEntityConfiguration<Message>
{
    public override void Configure(EntityTypeBuilder<Message> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.ConversationId, e.SentAt });
    }
}

public sealed class MessageReceiptConfiguration : TenantEntityConfiguration<MessageReceipt>
{
    public override void Configure(EntityTypeBuilder<MessageReceipt> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();
    }
}

public sealed class LeadConfiguration : TenantEntityConfiguration<Lead>
{
    public override void Configure(EntityTypeBuilder<Lead> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public sealed class AdmissionApplicationConfiguration : TenantEntityConfiguration<AdmissionApplication>
{
    public override void Configure(EntityTypeBuilder<AdmissionApplication> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.Status });
        b.HasIndex(e => new { e.CourseId, e.Status });
    }
}

public sealed class ApplicationDocumentConfiguration : TenantEntityConfiguration<ApplicationDocument>
{
    public override void Configure(EntityTypeBuilder<ApplicationDocument> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.ApplicationId, e.DocumentType });
    }
}

public sealed class FollowUpConfiguration : TenantEntityConfiguration<FollowUp>
{
    public override void Configure(EntityTypeBuilder<FollowUp> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.Status, e.ScheduledAt });
    }
}

public sealed class AssetConfiguration : TenantEntityConfiguration<Asset>
{
    public override void Configure(EntityTypeBuilder<Asset> b)
    {
        base.Configure(b);
        b.HasIndex(e => e.StorageKey).IsUnique();
        b.HasIndex(e => new { e.OwnerType, e.OwnerId });
    }
}
public sealed class TenantConfiguration : GlobalEntityConfiguration<Tenant>
{
    public override void Configure(EntityTypeBuilder<Tenant> b)
    {
        base.Configure(b);
        b.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(e => e.Slug).IsUnique();
        b.HasIndex(e => e.Subdomain).IsUnique();
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        b.Property(e => e.DefaultCurrency).HasMaxLength(3).HasDefaultValue("BDT");
    }
}

public sealed class PermissionConfiguration : GlobalEntityConfiguration<Permission>
{
    public override void Configure(EntityTypeBuilder<Permission> b)
    {
        base.Configure(b);
        b.HasIndex(e => e.Code).IsUnique();
        b.Property(e => e.Code).HasMaxLength(120).IsRequired();
        b.Property(e => e.Module).HasMaxLength(80).IsRequired();
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.HasIndex(e => new { e.TenantId, e.OccurredAt });
        b.HasIndex(e => new { e.EntityName, e.EntityId });
        b.Property(e => e.EntityName).HasMaxLength(120).IsRequired();
        b.Property(e => e.Action).HasMaxLength(20).IsRequired();
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.HasIndex(e => e.ProcessedAt);
        b.HasIndex(e => e.OccurredAt);
        b.Property(e => e.EventType).HasMaxLength(200).IsRequired();
    }
}

public sealed class UserConfiguration : GlobalEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> b)
    {
        base.Configure(b);
        b.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(e => e.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasIndex(e => e.PhoneNumber);
        b.Property(e => e.Email).HasMaxLength(256).IsRequired();
        b.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        b.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        b.Property(e => e.PasswordHash).HasMaxLength(512);
        b.Ignore(e => e.FullName);
    }
}

public sealed class RefreshTokenConfiguration : GlobalEntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        base.Configure(b);
        b.HasIndex(e => e.TokenHash).IsUnique();
        b.HasIndex(e => e.UserId);
        b.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
    }
}

public sealed class AttendanceStatConfiguration : TenantEntityConfiguration<AttendanceStat>
{
    public override void Configure(EntityTypeBuilder<AttendanceStat> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.StudentId, e.BatchId, e.Period })
         .IsUnique().HasFilter("\"IsDeleted\" = false");
        b.Property(e => e.AttendancePercent).HasColumnType("decimal(5,2)");
    }
}

public sealed class FeeSummaryConfiguration : TenantEntityConfiguration<FeeSummary>
{
    public override void Configure(EntityTypeBuilder<FeeSummary> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.StudentId, e.Period })
         .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class BatchUtilizationConfiguration : TenantEntityConfiguration<BatchUtilization>
{
    public override void Configure(EntityTypeBuilder<BatchUtilization> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.BatchId }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.Property(e => e.UtilizationPercent).HasColumnType("decimal(5,2)");
    }
}

public sealed class GradeSummaryConfiguration : TenantEntityConfiguration<GradeSummary>
{
    public override void Configure(EntityTypeBuilder<GradeSummary> b)
    {
        base.Configure(b);
        b.HasIndex(e => new { e.TenantId, e.StudentId, e.SubjectId })
         .IsUnique().HasFilter("\"IsDeleted\" = false");
        b.Property(e => e.Average).HasColumnType("decimal(6,2)");
    }
}
