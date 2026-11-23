using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Tenancy;
using CoachingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Api;

/// <summary>Idempotent bootstrap data: one demo center, its roles and the permission catalogue.</summary>
public static class DatabaseSeeder
{
    private static readonly string[] PermissionCodes =
    [
        "tenants.read", "tenants.write",
        "identity.user.read", "identity.user.write",
        "identity.role.read", "identity.role.write",
        "academics.course.read", "academics.course.write",
        "academics.batch.read", "academics.batch.write",
        "academics.enrollment.read", "academics.enrollment.write",
        "scheduling.timetable.read", "scheduling.timetable.write",
        "scheduling.attendance.read", "scheduling.attendance.write",
        "assessment.exam.read", "assessment.exam.write",
        "finance.invoice.read", "finance.invoice.write",
        "finance.payment.read", "finance.payment.write",
        "communication.announcement.read", "communication.announcement.write",
        "crm.lead.read", "crm.lead.write",
        "reporting.read"
    ];

    public static async Task SeedAsync(string connectionString, CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new AppDbContext(options, new StaticTenantContext());

        if (!await db.Tenants.AnyAsync(ct))
        {
            var tenant = new Tenant
            {
                Name = "Demo Coaching Center",
                Slug = "demo-center",
                Subdomain = "demo",
                ContactEmail = "admin@demo.local",
                Status = TenantStatus.Active,
                Plan = TenantPlan.Growth,
                TimeZoneId = "Asia/Dhaka",
                DefaultCurrency = "BDT"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);

            foreach (var code in PermissionCodes)
                db.Permissions.Add(new Permission { Code = code, Module = code.Split('.')[0] });
            await db.SaveChangesAsync(ct);

            var admin = new Role { TenantId = tenant.Id, Name = "Admin", IsSystemRole = true };
            var teacher = new Role { TenantId = tenant.Id, Name = "Teacher", IsSystemRole = true };
            var student = new Role { TenantId = tenant.Id, Name = "Student", IsSystemRole = true, IsDefault = true };
            var parent = new Role { TenantId = tenant.Id, Name = "Parent", IsSystemRole = true };
            db.Roles.AddRange(admin, teacher, student, parent);
            await db.SaveChangesAsync(ct);

            var permissions = await db.Permissions.ToListAsync(ct);
            foreach (var p in permissions)
                db.RolePermissions.Add(new RolePermission
                { TenantId = tenant.Id, RoleId = admin.Id, PermissionId = p.Id
                });
            await db.SaveChangesAsync(ct);
        }
    }
}
