using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Identity;

public sealed record InviteUser(string Email, string FirstName, string LastName, string? PhoneNumber,
    string? Password, IReadOnlyList<string> Roles) : IRequest<Guid>, IRequiresTenant;
public sealed class InviteUserValidator : AbstractValidator<InviteUser>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}
public sealed class InviteUserHandler(IApplicationDbContext db, ICurrentUser current, IPasswordHasher hasher)
    : IRequestHandler<InviteUser, Guid>
{
    public async Task<Guid> Handle(InviteUser r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        if (await db.Set<User>().AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("A user with this email already exists.");
        var user = new User
        {
            Email = email, FirstName = r.FirstName, LastName = r.LastName, PhoneNumber = r.PhoneNumber,
            EmailConfirmed = string.IsNullOrWhiteSpace(r.Password),
            PasswordHash = string.IsNullOrWhiteSpace(r.Password) ? null : hasher.Hash(r.Password),
            Status = UserStatus.Active
        };
        db.Set<User>().Add(user);
        db.Set<TenantMembership>().Add(new TenantMembership
        {
            TenantId = current.TenantId, UserId = user.Id,
            Status = MembershipStatus.Active, JoinedAt = DateTimeOffset.UtcNow
        });
        foreach (var name in r.Roles.Distinct())
        {
            var role = await db.Set<Role>().FirstOrDefaultAsync(x => x.Name == name, ct)
                       ?? throw new NotFoundException(nameof(Role), name);
            db.Set<UserRole>().Add(new UserRole
            {
                TenantId = current.TenantId, UserId = user.Id, RoleId = role.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
        return user.Id;
    }
}

public sealed record AssignUserRole(Guid UserId, string Role) : IRequest<bool>, IRequiresTenant;
public sealed class AssignUserRoleHandler(IApplicationDbContext db, ICurrentUser current) : IRequestHandler<AssignUserRole, bool>
{
    public async Task<bool> Handle(AssignUserRole r, CancellationToken ct)
    {
        var role = await db.Set<Role>().FirstOrDefaultAsync(x => x.Name == r.Role, ct)
                   ?? throw new NotFoundException(nameof(Role), r.Role);
        if (!await db.Set<TenantMembership>().AnyAsync(m => m.UserId == r.UserId && m.TenantId == current.TenantId, ct))
            throw new NotFoundException("Membership", r.UserId);
        if (await db.Set<UserRole>().AnyAsync(x => x.UserId == r.UserId && x.RoleId == role.Id, ct)) return false;
        db.Set<UserRole>().Add(new UserRole
        {
            TenantId = current.TenantId, UserId = r.UserId, RoleId = role.Id, AssignedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record RemoveUserRole(Guid UserId, string Role) : IRequest<bool>, IRequiresTenant;
public sealed class RemoveUserRoleHandler(IApplicationDbContext db) : IRequestHandler<RemoveUserRole, bool>
{
    public async Task<bool> Handle(RemoveUserRole r, CancellationToken ct)
    {
        var link = await (from ur in db.Set<UserRole>()
                          join role in db.Set<Role>() on ur.RoleId equals role.Id
                          where ur.UserId == r.UserId && role.Name == r.Role
                          select ur).FirstOrDefaultAsync(ct)
                    ?? throw new NotFoundException(nameof(UserRole), r.Role);
        db.Set<UserRole>().Remove(link);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record SetUserStatus(Guid UserId, UserStatus Status) : IRequest<bool>, IRequiresTenant;
public sealed class SetUserStatusHandler(IApplicationDbContext db) : IRequestHandler<SetUserStatus, bool>
{
    public async Task<bool> Handle(SetUserStatus r, CancellationToken ct)
    {
        var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == r.UserId, ct)
                   ?? throw new NotFoundException(nameof(User), r.UserId);
        user.Status = r.Status;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record ListUsers(string? Search, int Page, int PageSize)
    : IRequest<PagedResult<UserDto>>, IRequiresTenant;
public sealed record UserDto(Guid Id, string Email, string FullName, UserStatus Status, IReadOnlyList<string> Roles);
public sealed class ListUsersHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<ListUsers, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(ListUsers r, CancellationToken ct)
    {
        var memberIds = await db.Set<TenantMembership>()
            .Where(m => m.TenantId == current.TenantId).Select(m => m.UserId).ToListAsync(ct);
        var users = await db.Set<User>().Where(u => memberIds.Contains(u.Id)).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var term = r.Search.Trim();
            users = users.Where(u => u.Email.Contains(term) || u.FirstName.Contains(term) || u.LastName.Contains(term)).ToList();
        }
        var ids = users.Select(u => u.Id).ToList();
        var roles = await (from ur in db.Set<UserRole>() where ids.Contains(ur.UserId)
                           join role in db.Set<Role>() on ur.RoleId equals role.Id
                           select new { ur.UserId, role.Name }).ToListAsync(ct);
        var page = Math.Max(1, r.Page); var size = Math.Clamp(r.PageSize, 1, 100);
        var items = users.OrderBy(u => u.Email).Skip((page - 1) * size).Take(size)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.Status,
                roles.Where(x => x.UserId == u.Id).Select(x => x.Name).ToList())).ToList();
        return new PagedResult<UserDto>(items, users.Count, page, size);
    }
}
