namespace CoachingPlatform.Application.Outbox;

public static class EventTypes
{
    public const string TenantProvisioned = "tenant.provisioned";
    public const string EnrollmentCreated = "enrollment.created";
    public const string AttendanceMarked = "attendance.marked";
    public const string AssessmentSubmitted = "assessment.submitted";
    public const string InvoiceCreated = "invoice.created";
    public const string PaymentReceived = "payment.received";
    public const string SessionsGenerated = "scheduling.sessions_generated";
}
