using CoachingPlatform.Application.Features.Assessment;
using CoachingPlatform.Domain.Academics;
using CoachingPlatform.Domain.Assessment;
using CoachingPlatform.Domain.Common;
using CoachingPlatform.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachingPlatform.Tests.Integration;

public class RetakeTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Retake_DoesNotDuplicateGradebookRow()
    {
        await NewTenantAsync();
        var student = await NewUserAsync();
        var (courseId, _) = await NewCourseAndYearAsync();
        var subject = new Subject { CourseId = courseId, Code = "S", Name = "Subject" };
        Db.Subjects.Add(subject);
        await Db.SaveChangesAsync();
        var assessmentId = await Sender.Send(new CreateAssessment(subject.Id, null,
            AssessmentType.Quiz, "Quiz", null, null, null, null, 10m, 5m));
        var assessment = await Db.Assessments.SingleAsync(a => a.Id == assessmentId);
        assessment.Status = AssessmentStatus.Published;
        assessment.AllowRetakes = true;
        assessment.MaxAttempts = 3;
        await Db.SaveChangesAsync();

        var question = new Question
        {
            SubjectId = subject.Id, Type = QuestionType.SingleChoice, Difficulty = Difficulty.Easy,
            Text = "1+1?", OptionsJson = "[{\"id\":\"a\"}]",
            CorrectAnswerJson = "{\"value\":\"a\"}", Marks = 10m
        };
        Db.Questions.Add(question);
        await Db.SaveChangesAsync();
        Db.AssessmentQuestions.Add(new AssessmentQuestion
        {
            AssessmentId = assessmentId, QuestionId = question.Id, Marks = 10m, Order = 1
        });
        await Db.SaveChangesAsync();

        await Sender.Send(new SubmitAttempt(assessmentId, student,
            new[] { new AnswerInput(question.Id, "{\"value\":\"a\"}") }));
        await Sender.Send(new SubmitAttempt(assessmentId, student,
            new[] { new AnswerInput(question.Id, "{\"value\":\"a\"}") }));

        Assert.Equal(2, await Db.AssessmentAttempts.CountAsync(a => a.AssessmentId == assessmentId));
        Assert.Equal(1, await Db.GradeEntries.CountAsync(g => g.AssessmentId == assessmentId && g.StudentId == student));
    }
}
