using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Tenancy;

namespace CoachingPlatform.Domain.Identity;

/// <summary>
/// Globally unique person. A single human can belong to many tenants via TenantMembership.
/// </summary>
public sealed class User : GlobalEntity, IConcurrent
{
    public string Email { get; set; } = default!;        // stored lower-cased + unique index
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string? PasswordHash { get; set; }            // null for SSO-only users
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? AvatarAssetKey { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;
    public DateOnly? DateOfBirth { get; set; }
    public string? TimeZoneId { get; set; }
    public string? PreferredLanguage { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Invited;
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }               // encrypted at rest by infrastructure
    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>Joins a User to a Tenant with a lifecycle status.</summary>
public sealed class TenantMembership : TenantEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTimeOffset JoinedAt { get; set; }
    public string? EmployeeCode { get; set; }
    public string? Designation { get; set; }
}

public enum MembershipStatus { Invited, Active, Suspended, Left }

/// <summary>Tenant-scoped role (Owner, Admin, Teacher, Student, Parent, Accountant...).</summary>
public sealed class Role : TenantEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsDefault { get; set; }        // assigned automatically on enrolment
}

/// <summary>Global permission catalogue — stable string codes survive renames.</summary>
public sealed class Permission : GlobalEntity
{
    public string Code { get; set; } = default!;   // "academics.batch.write"
    public string Module { get; set; } = default!; // "Academics"
    public string? Description { get; set; }
}

public sealed class RolePermission : TenantEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

public sealed class UserRole : TenantEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
    public Guid? AssignedBy { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>Rotating refresh tokens; hashed at rest.</summary>
public sealed class RefreshToken : GlobalEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>Parent/guardian link. Permission flags support granular parent-portal access.</summary>
public sealed class StudentGuardian : TenantEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = default!;
    public Guid GuardianId { get; set; }
    public User Guardian { get; set; } = default!;
    public GuardianRelationship Relationship { get; set; } = GuardianRelationship.Guardian;
    public bool IsPrimary { get; set; }
    public bool CanViewGrades { get; set; } = true;
    public bool CanViewFees { get; set; } = true;
    public bool CanPickUp { get; set; } = true;
    public bool CanReceiveNotifications { get; set; } = true;
}
