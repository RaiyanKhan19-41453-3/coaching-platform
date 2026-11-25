using CoachingPlatform.Application.Features.Communication;
using CoachingPlatform.Application.Features.Academics;
using CoachingPlatform.Application.Features.Crm;
using CoachingPlatform.Application.Features.Identity;
using CoachingPlatform.Application.Features.Scheduling;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Domain.Identity;
using CoachingPlatform.Domain.Scheduling;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class NewFeaturesTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{

    private async Task SeedRolesAsync()
    {
        foreach (var name in new[] { "Admin", "Teacher", "Student", "Parent" })
            Db.Roles.Add(new Role { TenantId = Ambient.TenantId, Name = name });
        await Db.SaveChangesAsync();
    }

    [Fact]
    public async Task AdmissionsPipeline_ConvertsApplicationToStudent()
    {
        await NewTenantAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "B", "B-ADM",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 20));
        var leadId = await Sender.Send(new CreateLead("Jane", "Doe", "jane@admission.local",
            null, LeadSource.Website, null, courseId, null));
        var appId = await Sender.Send(new CreateApplication(leadId, courseId, yearId,
            "Jane", "Doe", "jane@admission.local", null, "John Doe", null));
        await Sender.Send(new AddApplicationDocument(appId, "NID", "nid.pdf", "keys/nid.pdf"));
        await Sender.Send(new ReviewApplication(appId, ApplicationStatus.Accepted, "Good", null));
        var userId = await Sender.Send(new EnrollApplicant(appId, batchId, "Password123!", null));

        Assert.NotEqual(Guid.Empty, userId);
        Assert.True(await Db.Users.AnyAsync(u => u.Id == userId && u.Email == "jane@admission.local"));
        Assert.True(await Db.Enrollments.AnyAsync(e => e.StudentId == userId && e.BatchId == batchId));
        Assert.True(await Db.ApplicationDocuments.AnyAsync(d => d.ApplicationId == appId));
        Assert.Equal(LeadStatus.Converted, (await Db.Leads.SingleAsync(l => l.Id == leadId)).Status);
    }

    [Fact]
    public async Task UserManagement_InvitesAndAssignsRole()
    {
        await NewTenantAsync();
        await SeedRolesAsync();
        var id = await Sender.Send(new InviteUser("teacher@test.local", "Tom", "Teacher", null,
            "Password123!", new[] { "Teacher" }));
        Assert.True(await Db.Users.AnyAsync(u => u.Id == id));
        Assert.True(await Db.UserRoles.AnyAsync(r => r.UserId == id));
        Assert.True(await Sender.Send(new AssignUserRole(id, "Admin")));
        Assert.True(await Sender.Send(new SetUserStatus(id, UserStatus.Suspended)));
        Assert.Equal(UserStatus.Suspended, (await Db.Users.SingleAsync(u => u.Id == id)).Status);
    }

    [Fact]
    public async Task Timetable_RoomAndSessionQueriesWork()
    {
        await NewTenantAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "B", "B-TIME",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 10));
        var roomId = await Sender.Send(new CreateRoom("R101", "R101", ResourceType.Classroom, 30, null, null));
        Assert.True(await Sender.Send(new CheckRoomAvailability(roomId,
            new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 2, 1, 11, 0, 0, TimeSpan.Zero))));
        Db.ClassSessions.Add(new ClassSession
        {
            BatchId = batchId, RoomId = roomId,
            StartUtc = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero),
            EndUtc = new DateTimeOffset(2026, 2, 1, 11, 0, 0, TimeSpan.Zero)
        });
        await Db.SaveChangesAsync();
        Assert.False(await Sender.Send(new CheckRoomAvailability(roomId,
            new DateTimeOffset(2026, 2, 1, 10, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 2, 1, 11, 30, 0, TimeSpan.Zero))));
        Assert.Single(await Sender.Send(new GetTimetable(batchId, null, null, null, null)));
    }

    [Fact]
    public async Task Announcement_FansOutToTargetedRole()
    {
        await NewTenantAsync();
        await SeedRolesAsync();
        var studentRole = await Db.Roles.SingleAsync(r => r.Name == "Student");
        var student = await NewUserAsync();
        Db.UserRoles.Add(new UserRole
        {
            TenantId = Ambient.TenantId, UserId = student, RoleId = studentRole.Id,
            AssignedAt = DateTimeOffset.UtcNow
        });
        await Db.SaveChangesAsync();

        await Sender.Send(new CreateAnnouncement("Exam tomorrow", "Bring your ID",
            "{\"roles\":[\"Student\"]}", AnnouncementPriority.High, true, null));
        var announcementId = (await Db.Announcements.SingleAsync()).Id;
        await Sender.Send(new FanOutAnnouncement(announcementId));

        Assert.True(await Db.Notifications.AnyAsync(n =>
            n.UserId == student && n.Type == "announcement.published"));
        Assert.True(await Db.OutboxMessages.AnyAsync(m => m.EventType == "notification.created"));
    }
}
