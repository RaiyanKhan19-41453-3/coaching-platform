namespace CoachingPlatform.Application.Common;

public interface IDocumentGenerator
{
    byte[] GenerateReportCard(ReportCardData data);
    byte[] GenerateInvoice(InvoiceDocumentData data);
}

public sealed record ReportCardData(string StudentName, string CourseName, decimal AttendancePercent,
    decimal AverageMarks, IReadOnlyList<ReportCardLine> Lines);

public sealed record ReportCardLine(string Subject, decimal Obtained, decimal Maximum);

public sealed record InvoiceDocumentData(string InvoiceNumber, string StudentName, DateOnly IssueDate,
    DateOnly DueDate, decimal SubTotal, decimal Discount, decimal Tax, decimal Total, string Currency);
