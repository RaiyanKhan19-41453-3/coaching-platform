namespace CoachingPlatform.Domain.Common;

public enum TenantStatus { Active, Suspended, Trial, Archived }
public enum TenantPlan { Free, Starter, Growth, Enterprise }
public enum UserStatus { Active, Invited, Suspended, Archived }
public enum Gender { Unknown, Male, Female, Other }

public enum CourseLevel { Beginner, Intermediate, Advanced, Professional }
public enum BatchStatus { Draft, Upcoming, Active, Completed, Cancelled }
public enum EnrollmentStatus { Applied, Waitlisted, Enrolled, Suspended, Transferred, Completed, Dropped }

public enum ResourceType { Classroom, Lab, Hall, Equipment, Other }
public enum SessionStatus { Scheduled, Ongoing, Completed, Cancelled, Rescheduled }
public enum AttendanceStatus { NotMarked, Present, Absent, Late, Excused }
public enum AttendanceMethod { Manual, QrCode, Geofence, Biometric, Imported }
public enum LeaveStatus { Pending, Approved, Rejected, Cancelled }

public enum QuestionType { SingleChoice, MultipleChoice, TrueFalse, ShortAnswer, Essay, Numeric, Coding }
public enum Difficulty { Easy, Medium, Hard }
public enum AssessmentType { Quiz, Assignment, Practice, Midterm, Final, Project }
public enum AssessmentStatus { Draft, Published, Ongoing, Completed, Archived }
public enum AttemptStatus { InProgress, Submitted, Grading, Graded, Disqualified }
public enum GradeCategory { Assignment, Quiz, Exam, Participation, Project, Behavior }

public enum FeeFrequency { OneTime, Monthly, Quarterly, Termly, Yearly }
public enum InvoiceStatus { Draft, Issued, PartiallyPaid, Paid, Overdue, Void, Refunded }
public enum PaymentMethod { Cash, Card, BankTransfer, MobileWallet, OnlineGateway, Cheque }
public enum PaymentStatus { Pending, Completed, Failed, Refunded, PartiallyRefunded }
public enum DiscountType { Percentage, FixedAmount }
public enum ExpenseStatus { Pending, Approved, Rejected, Paid }
public enum RefundStatus { Pending, Completed, Failed }

public enum NotificationChannel { InApp, Email, Sms, Push }
public enum DigestFrequency { Immediate, Hourly, Daily, Weekly }
public enum AnnouncementPriority { Low, Normal, High, Critical }
public enum ConversationType { Direct, Group, Batch, Support }
public enum ParticipantRole { Member, Admin, Owner }

public enum LeadSource { Website, Referral, WalkIn, SocialMedia, Advertisement, Other }
public enum LeadStatus { New, Contacted, Qualified, Converted, Lost }
public enum ApplicationStatus { Submitted, UnderReview, TestScheduled, Tested, Offered, Accepted, Rejected, Withdrawn, Enrolled }
public enum FollowUpStatus { Scheduled, Completed, Cancelled }

public enum AssetKind { Image, Pdf, Video, Audio, Document, Other }
public enum ScanStatus { Pending, Clean, Infected, Skipped }
public enum GuardianRelationship { Father, Mother, Guardian, Sibling, Other }
