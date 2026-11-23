using CoachingPlatform.Api.Jobs;
using CoachingPlatform.Application.Features.Academics;
using CoachingPlatform.Application.Features.Assessment;
using CoachingPlatform.Application.Features.Finance;
using CoachingPlatform.Application.Features.Identity;
using CoachingPlatform.Application.Features.Reporting;
using CoachingPlatform.Application.Features.Scheduling;
using CoachingPlatform.Domain.Common;
using MediatR;

namespace CoachingPlatform.Api;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Coaching Platform").RequireAuthorization();

        api.MapPost("/jobs/outbox/dispatch", async (OutboxDispatcher dispatcher, CancellationToken ct) =>
        {
            await dispatcher.DispatchCoreAsync(ct);
            return Results.Ok(new { dispatched = true });
        }).WithSummary("Manually drain the outbox (recurring job also runs every minute)");

        api.MapPost("/jobs/projections/rebuild", async (ProjectionRebuilder rebuilder, CancellationToken ct) =>
        {
            await rebuilder.RebuildAsync();
            return Results.Ok(new { rebuilt = true });
        }).WithSummary("Rebuild all reporting projections from source tables");

        api.MapGet("/auth/me", async (ISender sender) => Results.Ok(await sender.Send(new GetCurrentUser())));

        // ----- Reports (projection tables only) -----
        api.MapGet("/reports/dashboard", async (ISender sender) => Results.Ok(await sender.Send(new GetDashboardSummary())));
        api.MapGet("/reports/attendance", async (ISender sender, Guid? batchId, Guid? studentId, DateOnly? from, DateOnly? to)
            => Results.Ok(await sender.Send(new GetAttendanceReport(batchId, studentId, from, to))));
        api.MapGet("/reports/fees", async (ISender sender, Guid? studentId, DateOnly? from, DateOnly? to)
            => Results.Ok(await sender.Send(new GetFeeReport(studentId, from, to))));
        api.MapGet("/reports/utilization", async (ISender sender) => Results.Ok(await sender.Send(new GetUtilizationReport())));
        api.MapGet("/reports/grades", async (ISender sender, Guid? studentId, Guid? subjectId)
            => Results.Ok(await sender.Send(new GetGradeReport(studentId, subjectId))));

        // ----- Academics -----
        api.MapPost("/courses", async (ISender sender, CreateCourse cmd) => Results.Ok(await sender.Send(cmd)))
           .WithSummary("Create a course in the current center");
        api.MapGet("/batches", async (ISender sender, BatchStatus? status, string? search, int page = 1, int pageSize = 20)
            => Results.Ok(await sender.Send(new GetBatches(status, search, page == 0 ? 1 : page,
                pageSize == 0 ? 20 : pageSize))));
        api.MapPost("/batches", async (ISender sender, CreateBatch cmd) => Results.Ok(await sender.Send(cmd)));
        api.MapPost("/batches/{batchId:guid}/enrollments", async (ISender sender, Guid batchId, EnrollRequest body)
            => Results.Ok(await sender.Send(new EnrollStudent(batchId, body.StudentId))));

        // ----- Scheduling -----
        api.MapPost("/schedule-rules", async (ISender sender, CreateScheduleRule cmd) => Results.Ok(await sender.Send(cmd)));
        api.MapPost("/schedule-rules/{ruleId:guid}/sessions", async (ISender sender, Guid ruleId, GenerateRequest body)
            => Results.Ok(new { created = await sender.Send(new GenerateSessions(ruleId, body.From, body.To)) }));
        api.MapPost("/sessions/{sessionId:guid}/attendance", async (ISender sender, Guid sessionId, MarkAttendanceRequest body)
            => Results.Ok(new { written = await sender.Send(new MarkAttendance(sessionId, body.Records)) }));
        api.MapGet("/sessions/{sessionId:guid}/attendance", async (ISender sender, Guid sessionId)
            => Results.Ok(await sender.Send(new GetSessionAttendance(sessionId))));

        // ----- Assessment -----
        api.MapPost("/assessments", async (ISender sender, CreateAssessment cmd) => Results.Ok(await sender.Send(cmd)));
        api.MapPost("/assessments/{assessmentId:guid}/attempts", async (ISender sender, Guid assessmentId, SubmitRequest body)
            => Results.Ok(await sender.Send(new SubmitAttempt(assessmentId, body.StudentId, body.Answers))));

        // ----- Finance -----
        api.MapPost("/invoices", async (ISender sender, CreateInvoice cmd) => Results.Ok(await sender.Send(cmd)));
        api.MapPost("/invoices/{invoiceId:guid}/payments", async (ISender sender, Guid invoiceId, PayRequest body)
            => Results.Ok(await sender.Send(new RecordPayment(invoiceId, body.Amount, body.Currency, body.Method, body.GatewayTransactionId))));

        return app;
    }

    private sealed record EnrollRequest(Guid StudentId);
    private sealed record GenerateRequest(DateOnly From, DateOnly To);
    private sealed record MarkAttendanceRequest(IReadOnlyList<AttendanceMark> Records);
    private sealed record SubmitRequest(Guid StudentId, IReadOnlyList<AnswerInput> Answers);
    private sealed record PayRequest(decimal Amount, string Currency, PaymentMethod Method, string? GatewayTransactionId);
}
