using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Identity;

public sealed record InviteUserRequest(string Email, string FirstName, string LastName,
    string? PhoneNumber, string? Password, IReadOnlyList<string> Roles);
public sealed record AssignRoleRequest(string Role);
public sealed record SetUserStatusRequest(UserStatus Status);
public sealed record UserResponse(Guid Id, string Email, string FullName,
    UserStatus Status, IReadOnlyList<string> Roles);
public sealed record UserQuery(string? Search, int Page, int PageSize);
