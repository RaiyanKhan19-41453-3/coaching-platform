using CoachingPlatform.Application.Common;
using FluentValidation;

namespace CoachingPlatform.Api;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await Write(context, 400, "Validation failed",
                ex.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));
        }
        catch (AppException ex)
        {
            if (ex.StatusCode >= 500) logger.LogError(ex, "Application error");
            await Write(context, ex.StatusCode, ex.Title, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await Write(context, 500, "Internal server error", new { error = "An unexpected error occurred." });
        }
    }

    private static async Task Write(HttpContext context, int status, string title, object detail)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            status,
            title,
            detail,
            traceId = context.TraceIdentifier
        });
    }
}
