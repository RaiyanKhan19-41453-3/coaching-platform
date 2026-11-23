using CoachingPlatform.Domain.Common;

namespace CoachingPlatform.Domain.Reporting;

/// <summary>Monthly attendance rollup. Written by the outbox worker, never by request handlers.</summary>
public sealed class AttendanceStat : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid BatchId { get; set; }
    public DateOnly Period { get; set; }          // first day of the month
    public int Sessions { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Excused { get; set; }
    public decimal AttendancePercent { get; set; }
}

/// <summary>Monthly money in / money out per student, used for fee dashboards.</summary>
public sealed class FeeSummary : TenantEntity
{
    public Guid StudentId { get; set; }
    public DateOnly Period { get; set; }
    public Money Invoiced { get; set; } = Money.Zero();
    public Money Paid { get; set; } = Money.Zero();
    public Money Outstanding { get; set; } = Money.Zero();
    public int InvoiceCount { get; set; }
}

/// <summary>Denormalised seat utilization for a batch.</summary>
public sealed class BatchUtilization : TenantEntity
{
    public Guid BatchId { get; set; }
    public int Capacity { get; set; }
    public int Enrolled { get; set; }
    public int Waitlisted { get; set; }
    public decimal UtilizationPercent { get; set; }
}

/// <summary>Aggregate marks per student and subject for report cards and ranking.</summary>
public sealed class GradeSummary : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public decimal Average { get; set; }
    public decimal Highest { get; set; }
    public decimal Lowest { get; set; }
    public int Assessments { get; set; }
    public decimal MarksObtained { get; set; }
    public decimal MaxMarks { get; set; }
}
