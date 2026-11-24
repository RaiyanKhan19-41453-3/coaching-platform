using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Application.DTOs.Scheduling;

public sealed record CreateRoomRequest(string Name, string? Code, ResourceType Type,
    int Capacity, string? Building, string? Floor);
public sealed record RoomResponse(Guid Id, string Name, string? Code, ResourceType Type,
    int Capacity, string? Building);
public sealed record TimetableQuery(Guid? BatchId, Guid? TeacherId, Guid? StudentId,
    DateTimeOffset? From, DateTimeOffset? To);
public sealed record SessionResponse(Guid Id, Guid BatchId, string? Title, DateTimeOffset StartUtc,
    DateTimeOffset EndUtc, SessionStatus Status, Guid? TeacherId, Guid? RoomId, Guid? TopicId);
public sealed record RoomAvailabilityRequest(DateTimeOffset StartUtc, DateTimeOffset EndUtc);
