using CoachingPlatform.Application.DTOs.Assessment;
using CoachingPlatform.Application.Features.Assessment;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IAssessmentService
{
    Task<Guid> CreateAssessmentAsync(CreateAssessmentRequest request, CancellationToken ct = default);
    Task<Guid> SubmitAttemptAsync(Guid assessmentId, SubmitAttemptRequest request, CancellationToken ct = default);
    Task<Guid> CreateQuestionAsync(CreateQuestionRequest request, CancellationToken ct = default);
    Task<bool> UpdateQuestionAsync(Guid id, UpdateQuestionRequest request, CancellationToken ct = default);
    Task<Guid> AddQuestionAsync(Guid assessmentId, AddQuestionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionResponse>> ListQuestionsAsync(Guid? subjectId, Guid? topicId, CancellationToken ct = default);
    Task<bool> GradeAnswerAsync(Guid attemptId, GradeAnswerRequest request, CancellationToken ct = default);
}

public sealed class AssessmentService(ISender sender) : IAssessmentService
{
    public Task<Guid> CreateAssessmentAsync(CreateAssessmentRequest request, CancellationToken ct)
        => sender.Send(new CreateAssessment(request.SubjectId, request.BatchId, request.Type,
            request.Title, request.Instructions, request.StartUtc, request.EndUtc,
            request.DurationMinutes, request.TotalMarks, request.PassingMarks), ct);

    public Task<Guid> CreateQuestionAsync(CreateQuestionRequest request, CancellationToken ct)
        => sender.Send(new CreateQuestion(request.SubjectId, request.TopicId, request.Type, request.Difficulty,
            request.Text, request.OptionsJson, request.CorrectAnswerJson, request.Explanation,
            request.Marks, request.NegativeMarks, request.TagsJson), ct);

    public Task<bool> UpdateQuestionAsync(Guid id, UpdateQuestionRequest request, CancellationToken ct)
        => sender.Send(new UpdateQuestion(id, request.Text, request.OptionsJson,
            request.CorrectAnswerJson, request.Explanation, request.Marks), ct);

    public Task<Guid> AddQuestionAsync(Guid assessmentId, AddQuestionRequest request, CancellationToken ct)
        => sender.Send(new AddQuestionToAssessment(assessmentId, request.QuestionId, request.Marks, request.Order), ct);

    public async Task<IReadOnlyList<QuestionResponse>> ListQuestionsAsync(Guid? subjectId, Guid? topicId, CancellationToken ct)
    {
        var rows = await sender.Send(new ListQuestions(subjectId, topicId), ct);
        return rows.Select(r => new QuestionResponse(r.Id, r.SubjectId, r.Type, r.Difficulty, r.Text, r.Marks)).ToList();
    }

    public Task<bool> GradeAnswerAsync(Guid attemptId, GradeAnswerRequest request, CancellationToken ct)
        => sender.Send(new GradeAttemptAnswer(attemptId, request.QuestionId, request.MarksAwarded, request.Feedback), ct);

    public Task<Guid> SubmitAttemptAsync(Guid assessmentId, SubmitAttemptRequest request, CancellationToken ct)
    {
        var answers = request.Answers.Select(a => new AnswerInput(a.QuestionId, a.ResponseJson)).ToList();
        return sender.Send(new SubmitAttempt(assessmentId, request.StudentId, answers), ct);
    }
}
