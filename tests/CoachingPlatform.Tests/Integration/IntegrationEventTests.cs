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
        var request = new RecordExternalNotification(Ambient.TenantId, userId,
            "exam.results_published", "Exam results published", "Results for exam X are now available.");

        var first = await Sender.Send(request);
        var second = await Sender.Send(request);

        Assert.Equal(first, second);
        Assert.Equal(1, await Db.Notifications.CountAsync(n =>
            n.UserId == userId && n.Type == "exam.results_published"));
    }
}
