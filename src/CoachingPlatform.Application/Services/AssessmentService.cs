using CoachingPlatform.Application.DTOs.Assessment;
using CoachingPlatform.Application.Features.Assessment;
using MediatR;

namespace CoachingPlatform.Application.Services;

public interface IAssessmentService
{
    Task<Guid> CreateAssessmentAsync(CreateAssessmentRequest request, CancellationToken ct = default);
    Task<Guid> SubmitAttemptAsync(Guid assessmentId, SubmitAttemptRequest request, CancellationToken ct = default);
}

public sealed class AssessmentService(ISender sender) : IAssessmentService
{
    public Task<Guid> CreateAssessmentAsync(CreateAssessmentRequest request, CancellationToken ct)
        => sender.Send(new CreateAssessment(request.SubjectId, request.BatchId, request.Type,
            request.Title, request.Instructions, request.StartUtc, request.EndUtc,
            request.DurationMinutes, request.TotalMarks, request.PassingMarks), ct);

    public Task<Guid> SubmitAttemptAsync(Guid assessmentId, SubmitAttemptRequest request, CancellationToken ct)
    {
        var answers = request.Answers.Select(a => new AnswerInput(a.QuestionId, a.ResponseJson)).ToList();
        return sender.Send(new SubmitAttempt(assessmentId, request.StudentId, answers), ct);
    }
}
