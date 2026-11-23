namespace CoachingPlatform.Application.DTOs.Reporting;

public sealed record AttendanceReportQuery(Guid? BatchId, Guid? StudentId, DateOnly? From, DateOnly? To);

public sealed record FeeReportQuery(Guid? StudentId, DateOnly? From, DateOnly? To);

public sealed record GradeReportQuery(Guid? StudentId, Guid? SubjectId);

public sealed record AttendanceReportItem(
    Guid StudentId, Guid BatchId, DateOnly Period, int Sessions,
    int Present, int Absent, int Late, int Excused, decimal AttendancePercent);

public sealed record FeeReportItem(
    Guid StudentId, DateOnly Period, decimal Invoiced, decimal Paid,
    decimal Outstanding, string Currency, int InvoiceCount);

public sealed record FeeReportResponse(
    IReadOnlyList<FeeReportItem> Rows, decimal TotalInvoiced,
    decimal TotalPaid, decimal TotalOutstanding, string Currency);

public sealed record UtilizationItem(
    Guid BatchId, int Capacity, int Enrolled, int Waitlisted, decimal UtilizationPercent);

public sealed record GradeReportItem(
    Guid StudentId, Guid SubjectId, decimal Average, decimal Highest,
    decimal Lowest, int Assessments, decimal MarksObtained, decimal MaxMarks);

public sealed record DashboardResponse(
    int Students, int Batches, decimal Collected, decimal Outstanding,
    decimal AttendancePercent, int LowAttendanceCount);
