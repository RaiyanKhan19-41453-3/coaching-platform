using System.Text.Json;
using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Platform;
using CoachingPlatform.Domain.Tenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Identity;

public sealed record ProvisionTenant(
    string Name, string Slug, string AdminEmail, string AdminPassword,
    string AdminFirstName, string AdminLastName, string? Subdomain = null) : IRequest<Guid>;

public sealed class ProvisionTenantValidator : AbstractValidator<ProvisionTenant>
{
    public ProvisionTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lower-case letters, numbers and hyphens.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
    }
}

/// <summary>
/// System-level command: creates a center, its admin and its default RBAC skeleton in one
/// transaction. Intentionally does not implement IRequiresTenant — there is no tenant yet.
/// </summary>
public sealed class ProvisionTenantHandler(
    IApplicationDbContext db, IPasswordHasher hasher) : IRequestHandler<ProvisionTenant, Guid>
{
    public async Task<Guid> Handle(ProvisionTenant request, CancellationToken ct)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await db.Set<Tenant>().AnyAsync(t => t.Slug == slug, ct))
            throw new ConflictException($"Tenant slug '{slug}' is already taken.");
        var email = request.AdminEmail.Trim().ToLowerInvariant();
        if (await db.Set<User>().AnyAsync(u => u.Email == email, ct))
            throw new ConflictException($"A user with email '{email}' already exists.");

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Subdomain = request.Subdomain?.Trim().ToLowerInvariant(),
            ContactEmail = email,
            Status = TenantStatus.Active,
            Plan = TenantPlan.Starter
        };
        var admin = new User
        {
            Email = email,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            FirstName = request.AdminFirstName.Trim(),
            LastName = request.AdminLastName.Trim(),
            PasswordHash = hasher.Hash(request.AdminPassword),
            Status = UserStatus.Active
        };
        var membership = new TenantMembership
        { TenantId = tenant.Id, UserId = admin.Id,
            Status = MembershipStatus.Active, JoinedAt = DateTimeOffset.UtcNow,
            Designation = "Center Admin"
        };
        db.Set<Tenant>().Add(tenant);
        db.Set<User>().Add(admin);
        db.Set<TenantMembership>().Add(membership);

        var roleNames = new[] { "Admin", "Teacher", "Student", "Parent" };
        var roles = roleNames.Select((name, i) => new Role
        { TenantId = tenant.Id, Name = name,
            IsSystemRole = true, IsDefault = name == "Student"
        }).ToList();
        db.Set<Role>().AddRange(roles);

        var permissions = await db.Set<Permission>().ToListAsync(ct);
        foreach (var p in permissions)
            db.Set<RolePermission>().Add(new RolePermission
            { TenantId = tenant.Id,
                RoleId = roles[0].Id, PermissionId = p.Id
            });
        db.Set<UserRole>().Add(new UserRole
        { TenantId = tenant.Id,
            UserId = admin.Id, RoleId = roles[0].Id, AssignedAt = DateTimeOffset.UtcNow
        });
        db.Set<OutboxMessage>().Add(new OutboxMessage
        { TenantId = tenant.Id,
            EventType = EventTypes.TenantProvisioned,
            PayloadJson = JsonSerializer.Serialize(new
            { tenant.Id, tenant.Slug, AdminUserId = admin.Id, admin.Email }),
            OccurredAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return tenant.Id;
    }
}
