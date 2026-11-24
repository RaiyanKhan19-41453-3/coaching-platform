using CoachingPlatform.Application.Common;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Scheduling;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachingPlatform.Application.Features.Scheduling;

public sealed record CreateRoom(string Name, string? Code, ResourceType Type, int Capacity,
    string? Building, string? Floor) : IRequest<Guid>, IRequiresTenant;
public sealed class CreateRoomValidator : AbstractValidator<CreateRoom>
{
    public CreateRoomValidator() { RuleFor(x => x.Name).NotEmpty(); RuleFor(x => x.Capacity).GreaterThan(0); }
}
public sealed class CreateRoomHandler(IApplicationDbContext db, ICurrentUser current) : IRequestHandler<CreateRoom, Guid>
{
    public async Task<Guid> Handle(CreateRoom r, CancellationToken ct)
    {
        var room = new Room
        {
            TenantId = current.TenantId, Name = r.Name, Code = r.Code, Type = r.Type,
            Capacity = r.Capacity, Building = r.Building, Floor = r.Floor, IsActive = true
        };
        db.Set<Room>().Add(room);
        await db.SaveChangesAsync(ct);
        return room.Id;
    }
}

public sealed record ListRooms() : IRequest<IReadOnlyList<RoomDto>>, IRequiresTenant;
public sealed record RoomDto(Guid Id, string Name, string? Code, ResourceType Type, int Capacity, string? Building);
public sealed class ListRoomsHandler(IApplicationDbContext db) : IRequestHandler<ListRooms, IReadOnlyList<RoomDto>>
{
    public async Task<IReadOnlyList<RoomDto>> Handle(ListRooms r, CancellationToken ct)
        => await db.Set<Room>().AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new RoomDto(x.Id, x.Name, x.Code, x.Type, x.Capacity, x.Building)).ToListAsync(ct);
}

public sealed record GetTimetable(Guid? BatchId, Guid? TeacherId, Guid? StudentId,
    DateTimeOffset? From, DateTimeOffset? To) : IRequest<IReadOnlyList<SessionDto>>, IRequiresTenant;
public sealed record SessionDto(Guid Id, Guid BatchId, string? Title, DateTimeOffset StartUtc,
    DateTimeOffset EndUtc, SessionStatus Status, Guid? TeacherId, Guid? RoomId, Guid? TopicId);
public sealed class GetTimetableHandler(IApplicationDbContext db) : IRequestHandler<GetTimetable, IReadOnlyList<SessionDto>>
{
    public async Task<IReadOnlyList<SessionDto>> Handle(GetTimetable r, CancellationToken ct)
    {
        var q = db.Set<ClassSession>().AsNoTracking().AsQueryable();
        if (r.BatchId.HasValue) q = q.Where(s => s.BatchId == r.BatchId.Value);
        if (r.TeacherId.HasValue) q = q.Where(s => s.TeacherId == r.TeacherId.Value);
        if (r.From.HasValue) q = q.Where(s => s.StartUtc >= r.From.Value);
        if (r.To.HasValue) q = q.Where(s => s.StartUtc <= r.To.Value);
        if (r.StudentId.HasValue)
        {
            var batchIds = await db.Set<Enrollment>()
                .Where(e => e.StudentId == r.StudentId.Value &&
                            e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Transferred)
                .Select(e => e.BatchId).ToListAsync(ct);
            q = q.Where(s => batchIds.Contains(s.BatchId));
        }
        return await q.OrderBy(s => s.StartUtc)
            .Select(s => new SessionDto(s.Id, s.BatchId, s.Title, s.StartUtc, s.EndUtc,
                s.Status, s.TeacherId, s.RoomId, s.TopicId)).Take(500).ToListAsync(ct);
    }
}

public sealed record CheckRoomAvailability(Guid RoomId, DateTimeOffset StartUtc, DateTimeOffset EndUtc)
    : IRequest<bool>, IRequiresTenant;
public sealed class CheckRoomAvailabilityHandler(IApplicationDbContext db)
    : IRequestHandler<CheckRoomAvailability, bool>
{
    public async Task<bool> Handle(CheckRoomAvailability r, CancellationToken ct)
        => !await db.Set<ClassSession>().AnyAsync(s =>
            s.RoomId == r.RoomId && !s.IsCancelled && s.StartUtc < r.EndUtc && s.EndUtc > r.StartUtc, ct);
}
