namespace CoachingPlatform.Application.DTOs.Documents;

public sealed record ReportCardRequest(Guid StudentId, Guid BatchId);

public sealed record InvoicePdfRequest(Guid InvoiceId);

public sealed record DocumentResponse(string FileName, string ContentType, byte[] Content);
