using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Identity;

public sealed record GetCurrentUser() : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<GetCurrentUser, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUser request, CancellationToken ct)
    {
        if (current.UserId is null) throw new ForbiddenException("No authenticated user.");
        var user = await db.Set<User>().FirstOrDefaultAsync(u => u.Id == current.UserId, ct)
                   ?? throw new NotFoundException(nameof(User), current.UserId);
        return new CurrentUserDto(user.Id, user.Email, user.FullName, current.TenantId, current.Roles);
    }
}

public sealed record CurrentUserDto(
    Guid UserId, string Email, string FullName, Guid TenantId, IReadOnlyList<string> Roles);
