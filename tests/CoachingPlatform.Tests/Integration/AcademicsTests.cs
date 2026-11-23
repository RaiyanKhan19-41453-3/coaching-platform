using CoachingPlatform.Application.Common;
using CoachingPlatform.Application.Features.Academics;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Tests.Fixtures;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class AcademicsTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CreateCourse_PersistsTenantScopedRow()
    {
        await NewTenantAsync();
        var id = await Sender.Send(new CreateCourse("MATH", "Math", null, CourseLevel.Beginner, 6));

        var course = await Db.Courses.SingleAsync(c => c.Id == id);
        Assert.Equal("MATH", course.Code);
        Assert.Equal(Ambient.TenantId, course.TenantId);

        var otherTenant = Guid.NewGuid();
        Ambient.TenantId = otherTenant;
        Assert.Empty(await Db.Courses.ToListAsync());
    }

    [Fact]
    public async Task CreateCourse_DuplicateCode_ThrowsConflict()
    {
        await NewTenantAsync();
        await Sender.Send(new CreateCourse("PHY", "Physics", null, CourseLevel.Beginner, 6));
        await Assert.ThrowsAsync<ConflictException>(() =>
            Sender.Send(new CreateCourse("PHY", "Physics 2", null, CourseLevel.Beginner, 6)));
    }

    [Fact]
    public async Task CreateCourse_InvalidInput_ThrowsValidationException()
    {
        await NewTenantAsync();
        await Assert.ThrowsAsync<ValidationException>(() =>
            Sender.Send(new CreateCourse("", "", null, CourseLevel.Beginner, 0)));
    }

    [Fact]
    public async Task CreateBatch_RequiresTenant()
    {
        Ambient.TenantId = Guid.Empty;
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Sender.Send(new CreateBatch(Guid.NewGuid(), Guid.NewGuid(), null, "B", "B",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), 10)));
    }

    [Fact]
    public async Task EnrollStudent_RejectsDuplicateAndOverCapacity()
    {
        await NewTenantAsync();
        var student1 = await NewUserAsync();
        var student2 = await NewUserAsync();
        var (courseId, yearId) = await NewCourseAndYearAsync();
        var batchId = await Sender.Send(new CreateBatch(courseId, yearId, null, "Batch 1", "B1",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), 1));

        await Sender.Send(new EnrollStudent(batchId, student1));
        await Assert.ThrowsAsync<ConflictException>(() => Sender.Send(new EnrollStudent(batchId, student1)));
        await Assert.ThrowsAsync<ConflictException>(() => Sender.Send(new EnrollStudent(batchId, student2)));

        Assert.Equal(1, await Db.Enrollments.CountAsync());
        Assert.Contains(await Db.OutboxMessages.ToListAsync(), m => m.EventType == "enrollment.created");
    }
}
