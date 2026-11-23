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
    public const string PaymentRefunded = "payment.refunded";
    public const string EnrollmentStatusChanged = "enrollment.status_changed";
    public const string NotificationCreated = "notification.created";
    public const string AssessmentGraded = "assessment.graded";
}
