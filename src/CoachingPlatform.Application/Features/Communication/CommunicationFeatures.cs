using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Outbox;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Communication;
using CoachingPlatform.Domain.Platform;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoachingPlatform.Application.Features.Communication;

public sealed record CreateAnnouncement(string Title, string Body, string? TargetAudienceJson,
    AnnouncementPriority Priority, bool IsPinned, DateTimeOffset? ExpiresAt)
    : IRequest<Guid>, IRequiresTenant;

public sealed class CreateAnnouncementValidator : AbstractValidator<CreateAnnouncement>
{
    public CreateAnnouncementValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class CreateAnnouncementHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<CreateAnnouncement, Guid>
{
    public async Task<Guid> Handle(CreateAnnouncement request, CancellationToken ct)
    {
        var announcement = new Announcement
        {
            TenantId = current.TenantId, AuthorId = current.UserId ?? Guid.Empty,
            Title = request.Title, Body = request.Body, TargetAudienceJson = request.TargetAudienceJson,
            Priority = request.Priority, IsPinned = request.IsPinned,
            PublishedAt = DateTimeOffset.UtcNow, ExpiresAt = request.ExpiresAt
        };
        db.Set<Announcement>().Add(announcement);
        await db.SaveChangesAsync(ct);

        // Fan out to the targeted audience (roles, specific users or batches).
        var audience = AnnouncementAudience.Parse(request.TargetAudienceJson);
        var recipientIds = await AnnouncementAudience.ResolveRecipientsAsync(db, current.TenantId, audience, ct);
        foreach (var userId in recipientIds.Take(1000))
        {
            db.Set<Notification>().Add(new Notification
            {
                TenantId = current.TenantId, UserId = userId,
                Channel = NotificationChannel.InApp, Type = "announcement.published",
                Title = request.Title, Body = request.Body,
                RelatedEntityType = nameof(Announcement), RelatedEntityId = announcement.Id,
                SentAt = DateTimeOffset.UtcNow
            });
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                TenantId = current.TenantId, EventType = EventTypes.NotificationCreated,
                PayloadJson = JsonSerializer.Serialize(new
                { UserId = userId, Type = "announcement.published", Title = request.Title }),
                OccurredAt = DateTimeOffset.UtcNow
            });
        }
        if (recipientIds.Count > 0) await db.SaveChangesAsync(ct);
        return announcement.Id;
    }
}

internal static class AnnouncementAudience
{
    public static Audience Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Audience([], [], []);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new Audience(
                Read(root, "roles"), Read(root, "userIds"), Read(root, "batchIds"));
        }
        catch (JsonException) { return new Audience([], [], []); }
    }

    private static List<string> Read(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
            : [];

internal static async Task<List<Guid>> ResolveRecipientsAsync(
    IApplicationDbContext db, Guid tenantId, AnnouncementAudience.Audience audience, CancellationToken ct)
{
    var ids = new HashSet<Guid>();
    foreach (var raw in audience.UserIds) if (Guid.TryParse(raw, out var id)) ids.Add(id);

    if (audience.Roles.Count > 0)
    {
        var matches = await (from ur in db.Set<UserRole>()
                             join role in db.Set<Role>() on ur.RoleId equals role.Id
                             where audience.Roles.Contains(role.Name)
                             select ur.UserId).ToListAsync(ct);
        foreach (var id in matches) ids.Add(id);
    }
    if (audience.BatchIds.Count > 0)
    {
        var batchIds = audience.BatchIds.Where(x => Guid.TryParse(x, out _)).Select(Guid.Parse).ToList();
        var students = await db.Set<Enrollment>()
            .Where(e => batchIds.Contains(e.BatchId)).Select(e => e.StudentId).ToListAsync(ct);
        foreach (var id in students) ids.Add(id);
    }
    if (audience.Roles.Count == 0 && audience.UserIds.Count == 0 && audience.BatchIds.Count == 0)
    {
        var all = await db.Set<TenantMembership>()
            .Where(m => m.TenantId == tenantId).Select(m => m.UserId).ToListAsync(ct);
        foreach (var id in all) ids.Add(id);
    }
    return ids.ToList();
}

    internal sealed record Audience(List<string> Roles, List<string> UserIds, List<string> BatchIds);
}

public sealed record ListAnnouncements(int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<AnnouncementDto>>, IRequiresTenant;

public sealed record AnnouncementDto(Guid Id, string Title, string Body,
    AnnouncementPriority Priority, bool IsPinned, DateTimeOffset? PublishedAt);

public sealed class ListAnnouncementsHandler(IApplicationDbContext db)
    : IRequestHandler<ListAnnouncements, IReadOnlyList<AnnouncementDto>>
{
    public async Task<IReadOnlyList<AnnouncementDto>> Handle(ListAnnouncements request, CancellationToken ct)
        => await db.Set<Announcement>().AsNoTracking()
            .Where(a => a.PublishedAt != null && (a.ExpiresAt == null || a.ExpiresAt > DateTimeOffset.UtcNow))
            .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.PublishedAt)
            .Skip((Math.Max(1, request.Page) - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.Priority, a.IsPinned, a.PublishedAt))
            .ToListAsync(ct);
}

public sealed record SendMessage(Guid? ConversationId, Guid RecipientId, string Body)
    : IRequest<Guid>, IRequiresTenant;

public sealed class SendMessageValidator : AbstractValidator<SendMessage>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}

public sealed class SendMessageHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<SendMessage, Guid>
{
    public async Task<Guid> Handle(SendMessage request, CancellationToken ct)
    {
        var senderId = current.UserId ?? throw new ForbiddenException("No authenticated user.");
        if (!await db.Set<Domain.Identity.User>().AnyAsync(u => u.Id == request.RecipientId, ct))
            throw new NotFoundException(nameof(Domain.Identity.User), request.RecipientId);
        Conversation? conversation;
        if (request.ConversationId.HasValue)
        {
            conversation = await db.Set<Conversation>()
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, ct)
                ?? throw new NotFoundException(nameof(Conversation), request.ConversationId.Value);
            if (!await db.Set<ConversationParticipant>().AnyAsync(p =>
                    p.ConversationId == conversation.Id && p.UserId == senderId, ct))
                throw new ForbiddenException("Not a participant in this conversation.");
        }
        else
        {
            conversation = new Conversation
            {
                TenantId = current.TenantId, Type = ConversationType.Direct, CreatedBy = senderId
            };
            db.Set<Conversation>().Add(conversation);
            db.Set<ConversationParticipant>().Add(new ConversationParticipant
            {
                TenantId = current.TenantId, ConversationId = conversation.Id,
                UserId = senderId, Role = ParticipantRole.Owner, JoinedAt = DateTimeOffset.UtcNow
            });
            db.Set<ConversationParticipant>().Add(new ConversationParticipant
            {
                TenantId = current.TenantId, ConversationId = conversation.Id,
                UserId = request.RecipientId, Role = ParticipantRole.Member, JoinedAt = DateTimeOffset.UtcNow
            });
        }

        var message = new Message
        {
            TenantId = current.TenantId, ConversationId = conversation.Id,
            SenderId = senderId, Body = request.Body, SentAt = DateTimeOffset.UtcNow
        };
        db.Set<Message>().Add(message);
        conversation.LastMessageAt = message.SentAt;

        db.Set<Notification>().Add(new Notification
        {
            TenantId = current.TenantId, UserId = request.RecipientId,
            Channel = NotificationChannel.InApp, Type = "chat.message", Title = "New message",
            Body = request.Body.Length > 120 ? request.Body[..120] : request.Body,
            SentAt = DateTimeOffset.UtcNow
        });
        db.Set<OutboxMessage>().Add(new OutboxMessage
        {
            TenantId = current.TenantId, EventType = EventTypes.NotificationCreated,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            { UserId = request.RecipientId, Type = "chat.message", Title = "New message" }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return message.Id;
    }
}

public sealed record ListConversations() : IRequest<IReadOnlyList<ConversationDto>>, IRequiresTenant;

public sealed record ConversationDto(Guid Id, ConversationType Type, string? Title, DateTimeOffset? LastMessageAt);

public sealed class ListConversationsHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<ListConversations, IReadOnlyList<ConversationDto>>
{
    public async Task<IReadOnlyList<ConversationDto>> Handle(ListConversations request, CancellationToken ct)
    {
        if (current.UserId is null) throw new ForbiddenException("No authenticated user.");
        return await (from c in db.Set<Conversation>()
                      join p in db.Set<ConversationParticipant>() on c.Id equals p.ConversationId
                      where p.UserId == current.UserId.Value
                      select new ConversationDto(c.Id, c.Type, c.Title, c.LastMessageAt)).ToListAsync(ct);
    }
}

public sealed record GetMessages(Guid ConversationId) : IRequest<IReadOnlyList<MessageDto>>, IRequiresTenant;

public sealed record MessageDto(Guid Id, Guid SenderId, string? Body, DateTimeOffset SentAt);

public sealed class GetMessagesHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<GetMessages, IReadOnlyList<MessageDto>>
{
    public async Task<IReadOnlyList<MessageDto>> Handle(GetMessages request, CancellationToken ct)
    {
        if (!await db.Set<ConversationParticipant>().AnyAsync(p =>
                p.ConversationId == request.ConversationId && p.UserId == current.UserId, ct))
            throw new ForbiddenException("Not a participant in this conversation.");
        return await db.Set<Message>().AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId)
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto(m.Id, m.SenderId, m.Body, m.SentAt)).ToListAsync(ct);
    }
}

public sealed record MarkConversationRead(Guid ConversationId) : IRequest<bool>, IRequiresTenant;

public sealed class MarkConversationReadHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<MarkConversationRead, bool>
{
    public async Task<bool> Handle(MarkConversationRead request, CancellationToken ct)
    {
        var participant = await db.Set<ConversationParticipant>().FirstOrDefaultAsync(p =>
            p.ConversationId == request.ConversationId && p.UserId == current.UserId, ct)
            ?? throw new NotFoundException(nameof(ConversationParticipant), request.ConversationId);
        participant.LastReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record UpdateNotificationPreference(NotificationChannel Channel, string Type,
    bool IsEnabled, DigestFrequency DigestFrequency) : IRequest<bool>, IRequiresTenant;

public sealed class UpdateNotificationPreferenceHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<UpdateNotificationPreference, bool>
{
    public async Task<bool> Handle(UpdateNotificationPreference request, CancellationToken ct)
    {
        if (current.UserId is null) throw new ForbiddenException("No authenticated user.");
        var pref = await db.Set<NotificationPreference>().FirstOrDefaultAsync(p =>
            p.UserId == current.UserId.Value && p.Channel == request.Channel && p.Type == request.Type, ct);
        if (pref is null)
        {
            pref = new NotificationPreference
            {
                TenantId = current.TenantId, UserId = current.UserId.Value,
                Channel = request.Channel, Type = request.Type
            };
            db.Set<NotificationPreference>().Add(pref);
        }
        pref.IsEnabled = request.IsEnabled;
        pref.DigestFrequency = request.DigestFrequency;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record ListNotifications(int Page = 1, int PageSize = 50)
    : IRequest<IReadOnlyList<NotificationDto>>, IRequiresTenant;

public sealed record NotificationDto(Guid Id, string Type, string Title, string Body, bool IsRead, DateTimeOffset? SentAt);

public sealed class ListNotificationsHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<ListNotifications, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(ListNotifications request, CancellationToken ct)
    {
        if (current.UserId is null) throw new ForbiddenException("No authenticated user.");
        return await db.Set<Notification>().AsNoTracking()
            .Where(n => n.UserId == current.UserId.Value)
            .OrderByDescending(n => n.SentAt)
            .Skip((Math.Max(1, request.Page) - 1) * request.PageSize).Take(request.PageSize)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.SentAt))
            .ToListAsync(ct);
    }
}

public sealed record MarkNotificationRead(Guid NotificationId) : IRequest<bool>, IRequiresTenant;

public sealed class MarkNotificationReadHandler(IApplicationDbContext db, ICurrentUser current)
    : IRequestHandler<MarkNotificationRead, bool>
{
    public async Task<bool> Handle(MarkNotificationRead request, CancellationToken ct)
    {
        var notice = await db.Set<Notification>().FirstOrDefaultAsync(n =>
            n.Id == request.NotificationId && n.UserId == current.UserId, ct)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);
        notice.IsRead = true;
        notice.ReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
