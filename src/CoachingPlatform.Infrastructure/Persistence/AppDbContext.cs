using System.Linq.Expressions;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Domain.Content;
using CoachingPlatform.Domain.Crm;
using CoachingPlatform.Domain.Finance;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Domain.Scheduling;
using CoachingPlatform.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantContext tenantContext,
        TimeProvider? timeProvider = null) : base(options)
    {
        _tenantContext = tenantContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Referenced by the dynamically built global query filters.</summary>
    public Guid CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    public DbSet<User> Users => Set<User>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();

    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchTeacher> BatchTeachers => Set<BatchTeacher>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<BatchTopic> BatchTopics => Set<BatchTopic>();
    public DbSet<LearningMaterial> LearningMaterials => Set<LearningMaterial>();

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<ScheduleRule> ScheduleRules => Set<ScheduleRule>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<GradeEntry> GradeEntries => Set<GradeEntry>();
    public DbSet<Rubric> Rubrics => Set<Rubric>();
    public DbSet<RubricCriterion> RubricCriteria => Set<RubricCriterion>();

    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReceipt> MessageReceipts => Set<MessageReceipt>();

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DateTimeOffset Now => _timeProvider.GetUtcNow();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureOwnedValueObjects(modelBuilder);
        ApplyGlobalQueryFilters(modelBuilder);

        // Restrict everywhere: domain services decide deletion semantics; never cascade by accident.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static void ConfigureOwnedValueObjects(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (entityType.ClrType == typeof(Money)) continue;
            foreach (var property in entityType.ClrType.GetProperties())
            {
                if (property.PropertyType != typeof(Money)) continue;
                modelBuilder.Entity(entityType.ClrType).OwnsOne(typeof(Money), property.Name, owned =>
                {
                    owned.Property(nameof(Money.Amount)).HasColumnType("decimal(18,2)").IsRequired();
                    owned.Property(nameof(Money.Currency)).HasMaxLength(3).IsRequired();
                });
            }
        }
    }

    /// <summary>
    /// Builds one filter per entity: soft-delete + tenant isolation.
    /// Tenant id is read from the context instance, so EF parameterises it per query.
    /// </summary>
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        var tenantIdProperty = Expression.Property(
            Expression.Constant(this), nameof(CurrentTenantId));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (!typeof(ITenantEntity).IsAssignableFrom(clr) &&
                !typeof(ISoftDeletable).IsAssignableFrom(clr)) continue;

            var parameter = Expression.Parameter(clr, "e");
            Expression body = Expression.Constant(true);

            if (typeof(ISoftDeletable).IsAssignableFrom(clr))
            {
                var isDeleted = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                body = Expression.AndAlso(body,
                    Expression.Equal(isDeleted, Expression.Constant(false)));
            }

            if (typeof(ITenantEntity).IsAssignableFrom(clr))
            {
                var rowTenantId = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                body = Expression.AndAlso(body, Expression.Equal(rowTenantId, tenantIdProperty));
            }

            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
