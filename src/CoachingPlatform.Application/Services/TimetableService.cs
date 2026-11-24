using CoachingPlatform.Application.DTOs.Scheduling;
using CoachingPlatform.Application.Features.Scheduling;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface ITimetableService
{
    Task<Guid> CreateRoomAsync(CreateRoomRequest r, CancellationToken ct = default);
    Task<IReadOnlyList<RoomResponse>> ListRoomsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SessionResponse>> GetTimetableAsync(TimetableQuery q, CancellationToken ct = default);
    Task<bool> IsRoomAvailableAsync(Guid roomId, RoomAvailabilityRequest r, CancellationToken ct = default);
}

public sealed class TimetableService(ISender sender) : ITimetableService
{
    public Task<Guid> CreateRoomAsync(CreateRoomRequest r, CancellationToken ct)
        => sender.Send(new CreateRoom(r.Name, r.Code, r.Type, r.Capacity, r.Building, r.Floor), ct);
    public async Task<IReadOnlyList<RoomResponse>> ListRoomsAsync(CancellationToken ct)
    {
        var rows = await sender.Send(new ListRooms(), ct);
        return rows.Select(x => new RoomResponse(x.Id, x.Name, x.Code, x.Type, x.Capacity, x.Building)).ToList();
    }
    public async Task<IReadOnlyList<SessionResponse>> GetTimetableAsync(TimetableQuery q, CancellationToken ct)
    {
        var rows = await sender.Send(new GetTimetable(q.BatchId, q.TeacherId, q.StudentId, q.From, q.To), ct);
        return rows.Select(x => new SessionResponse(x.Id, x.BatchId, x.Title, x.StartUtc, x.EndUtc,
            x.Status, x.TeacherId, x.RoomId, x.TopicId)).ToList();
    }
    public Task<bool> IsRoomAvailableAsync(Guid roomId, RoomAvailabilityRequest r, CancellationToken ct)
        => sender.Send(new CheckRoomAvailability(roomId, r.StartUtc, r.EndUtc), ct);
}
