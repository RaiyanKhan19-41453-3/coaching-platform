using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.DTOs.Communication;
using CoachingPlatform.Application.Features.Communication;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface ICommunicationService
{
    Task<Guid> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AnnouncementResponse>> ListAnnouncementsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Guid> SendMessageAsync(SendMessageRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ConversationResponse>> ListConversationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MessageResponse>> GetMessagesAsync(Guid conversationId, CancellationToken ct = default);
    Task<bool> MarkConversationReadAsync(Guid conversationId, CancellationToken ct = default);
    Task<bool> UpdatePreferenceAsync(UpdatePreferenceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationResponse>> ListNotificationsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<bool> MarkNotificationReadAsync(Guid notificationId, CancellationToken ct = default);
}

public sealed class CommunicationService(ISender sender) : ICommunicationService
{
    public Task<Guid> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken ct)
        => sender.Send(new CreateAnnouncement(request.Title, request.Body, request.TargetAudienceJson,
            request.Priority, request.IsPinned, request.ExpiresAt), ct);

    public async Task<IReadOnlyList<AnnouncementResponse>> ListAnnouncementsAsync(int page, int pageSize, CancellationToken ct)
    {
        var rows = await sender.Send(new ListAnnouncements(page, pageSize), ct);
        return rows.Select(r => new AnnouncementResponse(r.Id, r.Title, r.Body, r.Priority, r.IsPinned, r.PublishedAt)).ToList();
    }

    public Task<Guid> SendMessageAsync(SendMessageRequest request, CancellationToken ct)
        => sender.Send(new SendMessage(request.ConversationId, request.RecipientId, request.Body), ct);

    public async Task<IReadOnlyList<ConversationResponse>> ListConversationsAsync(CancellationToken ct)
    {
        var rows = await sender.Send(new ListConversations(), ct);
        return rows.Select(r => new ConversationResponse(r.Id, r.Type, r.Title, r.LastMessageAt)).ToList();
    }

    public async Task<IReadOnlyList<MessageResponse>> GetMessagesAsync(Guid conversationId, CancellationToken ct)
    {
        var rows = await sender.Send(new GetMessages(conversationId), ct);
        return rows.Select(r => new MessageResponse(r.Id, r.SenderId, r.Body, r.SentAt)).ToList();
    }

    public Task<bool> MarkConversationReadAsync(Guid conversationId, CancellationToken ct)
        => sender.Send(new MarkConversationRead(conversationId), ct);

    public Task<bool> UpdatePreferenceAsync(UpdatePreferenceRequest request, CancellationToken ct)
        => sender.Send(new UpdateNotificationPreference(request.Channel, request.Type, request.IsEnabled, request.DigestFrequency), ct);

    public async Task<IReadOnlyList<NotificationResponse>> ListNotificationsAsync(int page, int pageSize, CancellationToken ct)
    {
        var rows = await sender.Send(new ListNotifications(page, pageSize), ct);
        return rows.Select(r => new NotificationResponse(r.Id, r.Type, r.Title, r.Body, r.IsRead, r.SentAt)).ToList();
    }

    public Task<bool> MarkNotificationReadAsync(Guid notificationId, CancellationToken ct)
        => sender.Send(new MarkNotificationRead(notificationId), ct);
}
