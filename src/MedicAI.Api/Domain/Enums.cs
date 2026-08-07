namespace MedicAI.Api.Domain;

public enum UserRole
{
    Patient,
    Staff,
    Doctor,
    Admin
}

public enum UserStatus
{
    Active,
    Inactive,
    Locked
}

public enum AppointmentStatus
{
    Booked,
    CheckedIn,
    InConsultation,
    Completed,
    Cancelled,
    NoShow
}

public enum DurationUnit
{
    Hours,
    Days,
    Weeks,
    Months
}

public enum UrgencyLevel
{
    Routine,
    Priority,
    Urgent,
    EmergencyWarning
}

public enum AssessmentSource
{
    RuleEngine,
    MachineLearning,
    ManualFallback
}

public enum QueueStatus
{
    Waiting,
    Called,
    InConsultation,
    Completed,
    Removed
}

public enum AlertStatus
{
    Created,
    Acknowledged,
    Escalated,
    Resolved,
    CancelledAsFalseAlarm
}

public enum NotificationChannel
{
    InApp,
    Email,
    Sms,
    WhatsApp
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed
}
