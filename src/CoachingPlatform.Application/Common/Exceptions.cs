namespace CoachingPlatform.Application.Common;

public class AppException : Exception
{
    public AppException(string message, int statusCode = 400, string title = "Request failed") : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }
    public string Title { get; }
}

public sealed class NotFoundException(string entity, object key)
    : AppException($"{entity} '{key}' was not found.", 404, "Not found");

public sealed class ConflictException(string message)
    : AppException(message, 409, "Conflict");

public sealed class ForbiddenException(string message)
    : AppException(message, 403, "Forbidden");

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
