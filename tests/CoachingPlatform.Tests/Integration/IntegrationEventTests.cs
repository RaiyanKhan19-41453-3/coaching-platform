using CoachingPlatform.Application.Features.Integration;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class IntegrationEventTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DuplicateWebhookDelivery_DoesNotDuplicateNotification()
    {
        await NewTenantAsync();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var request = new RecordExternalNotification(Ambient.TenantId, userId,
            "exam.results_published", "Exam results published", "Results for exam X are now available.", eventId);

        var first = await Sender.Send(request);
        var second = await Sender.Send(request);

        Assert.Equal(first, second);
        Assert.Equal(1, await Db.Notifications.CountAsync(n =>
            n.UserId == userId && n.Type == "exam.results_published"));
        Assert.Equal(1, await Db.InboxMessages.CountAsync(i =>
            i.TenantId == Ambient.TenantId && i.EventId == $"{eventId:N}:{userId:N}"));
    }

    [Fact]
    public async Task ReplayedEventWithChangedPayload_IsStillIgnored()
    {
        await NewTenantAsync();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var first = await Sender.Send(new RecordExternalNotification(Ambient.TenantId, userId,
            "exam.results_published", "Exam results published", "Original body", eventId));

        // A retry after a payload change, or long after the old 1-hour window, must
        // still be a no-op when the producer's event id is unchanged.
        var replay = await Sender.Send(new RecordExternalNotification(Ambient.TenantId, userId,
            "exam.results_published", "Different title", "Rewritten body", eventId));

        Assert.Equal(first, replay);
        var notice = await Db.Notifications.SingleAsync(n => n.UserId == userId);
        Assert.Equal("Original body", notice.Body);
        Assert.Equal(1, await Db.InboxMessages.CountAsync(i =>
            i.TenantId == Ambient.TenantId && i.EventId == $"{eventId:N}:{userId:N}"));
    }

    [Fact]
    public async Task DifferentRecipientsOfSameEvent_EachGetOneNotification()
    {
        await NewTenantAsync();
        var eventId = Guid.NewGuid();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        await Sender.Send(new RecordExternalNotification(Ambient.TenantId, alice,
            "exam.results_published", "Exam results published", "Results are available.", eventId));
        await Sender.Send(new RecordExternalNotification(Ambient.TenantId, bob,
            "exam.results_published", "Exam results published", "Results are available.", eventId));
        await Sender.Send(new RecordExternalNotification(Ambient.TenantId, alice,
            "exam.results_published", "Exam results published", "Results are available.", eventId));

        Assert.Equal(2, await Db.Notifications.CountAsync(n =>
            n.Type == "exam.results_published" && (n.UserId == alice || n.UserId == bob)));
        Assert.Equal(2, await Db.InboxMessages.CountAsync(i => i.TenantId == Ambient.TenantId));
    }

    [Fact]
    public async Task LegacyWebhookWithoutEventId_StillDedupes()
    {
        await NewTenantAsync();
        var userId = Guid.NewGuid();
        var request = new RecordExternalNotification(Ambient.TenantId, userId,
            "exam.results_published", "Exam results published", "Legacy body");

        var first = await Sender.Send(request);
        var second = await Sender.Send(request);

        Assert.Equal(first, second);
        Assert.Equal(1, await Db.Notifications.CountAsync(n => n.UserId == userId));
        Assert.Equal(0, await Db.InboxMessages.CountAsync(i => i.TenantId == Ambient.TenantId));
    }
}
