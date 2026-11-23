namespace CoachingPlatform.Application.Common;

public interface ICurrentUser
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    string? UserEmail { get; }
    IReadOnlyList<string> Roles { get; }
}

/// <summary>Marks requests that must never run without a resolved tenant.</summary>
public interface IRequiresTenant;
