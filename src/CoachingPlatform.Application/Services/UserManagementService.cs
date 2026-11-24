using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.DTOs.Identity;
using CoachingPlatform.Application.Features.Identity;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IUserManagementService
{
    Task<Guid> InviteAsync(InviteUserRequest r, CancellationToken ct = default);
    Task<bool> AssignRoleAsync(Guid userId, AssignRoleRequest r, CancellationToken ct = default);
    Task<bool> RemoveRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<bool> SetStatusAsync(Guid userId, SetUserStatusRequest r, CancellationToken ct = default);
    Task<PagedResult<UserResponse>> ListAsync(UserQuery q, CancellationToken ct = default);
}

public sealed class UserManagementService(ISender sender) : IUserManagementService
{
    public Task<Guid> InviteAsync(InviteUserRequest r, CancellationToken ct)
        => sender.Send(new InviteUser(r.Email, r.FirstName, r.LastName, r.PhoneNumber, r.Password, r.Roles), ct);
    public Task<bool> AssignRoleAsync(Guid userId, AssignRoleRequest r, CancellationToken ct)
        => sender.Send(new AssignUserRole(userId, r.Role), ct);
    public Task<bool> RemoveRoleAsync(Guid userId, string role, CancellationToken ct)
        => sender.Send(new RemoveUserRole(userId, role), ct);
    public Task<bool> SetStatusAsync(Guid userId, SetUserStatusRequest r, CancellationToken ct)
        => sender.Send(new SetUserStatus(userId, r.Status), ct);
    public async Task<PagedResult<UserResponse>> ListAsync(UserQuery q, CancellationToken ct)
    {
        var result = await sender.Send(new ListUsers(q.Search, q.Page, q.PageSize), ct);
        var items = result.Items.Select(u => new UserResponse(u.Id, u.Email, u.FullName, u.Status, u.Roles)).ToList();
        return new PagedResult<UserResponse>(items, result.Total, result.Page, result.PageSize);
    }
}
