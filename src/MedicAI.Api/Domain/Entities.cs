using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicAI.Api.Domain;

public sealed class User
{
    public int Id { get; set; }

    [MaxLength(191)]
    public required string Email { get; set; }

    [MaxLength(255)]
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public PatientProfile? PatientProfile { get; set; }
    public StaffProfile? StaffProfile { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public sealed class PatientProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(150)]
    public required string FullName { get; set; }

    public DateOnly DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? Gender { get; set; }

    [MaxLength(30)]
    public required string Phone { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    public string? Allergies { get; set; }
    public string? ExistingConditions { get; set; }

    [MaxLength(150)]
    public string? EmergencyContact { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<SymptomSubmission> SymptomSubmissions { get; set; } = new List<SymptomSubmission>();
    public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
    public ICollection<EmergencyAlert> EmergencyAlerts { get; set; } = new List<EmergencyAlert>();
}

public sealed class StaffProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(150)]
    public required string FullName { get; set; }

    [MaxLength(100)]
    public required string Position { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }
}

public sealed class DoctorProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(150)]
    public required string FullName { get; set; }

    [MaxLength(80)]
    public required string RegistrationNo { get; set; }

    [MaxLength(120)]
    public string? Speciality { get; set; }

    public bool IsAvailable { get; set; } = true;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}

public sealed class Appointment
{
    public long Id { get; set; }
    public int PatientId { get; set; }
    public PatientProfile Patient { get; set; } = null!;
    public int? AssignedDoctorId { get; set; }
    public DoctorProfile? AssignedDoctor { get; set; }
    public DateTimeOffset AppointmentDateTime { get; set; }

    [MaxLength(100)]
    public required string ServiceType { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public SymptomSubmission? SymptomSubmission { get; set; }
    public QueueEntry? QueueEntry { get; set; }
    public Consultation? Consultation { get; set; }
}

public sealed class SymptomSubmission
{
    public long Id { get; set; }
    public int PatientId { get; set; }
    public PatientProfile Patient { get; set; } = null!;
    public long? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    [MaxLength(255)]
    public required string MainSymptom { get; set; }

    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public short PainSeverity { get; set; }
    public string? AdditionalSymptoms { get; set; }
    public string? ExistingConditionsSnapshot { get; set; }
    public bool ConsentConfirmed { get; set; }

    public bool Fever { get; set; }
    public bool Vomiting { get; set; }
    public bool Dizziness { get; set; }
    public bool WorseningSymptoms { get; set; }
    public bool SevereBreathingDifficulty { get; set; }
    public bool ChestPain { get; set; }
    public bool LossOfConsciousness { get; set; }
    public bool MajorBleeding { get; set; }
    public bool Seizure { get; set; }
    public bool SevereAllergicReaction { get; set; }
    public bool SuddenSevereWeakness { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public TriageAssessment? Assessment { get; set; }
}

public sealed class TriageAssessment
{
    public long Id { get; set; }
    public long SubmissionId { get; set; }
    public SymptomSubmission Submission { get; set; } = null!;
    public int? ModelVersionId { get; set; }
    public AiModelVersion? ModelVersion { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public AssessmentSource Source { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string? RuleFlagsJson { get; set; }
    public required string Explanation { get; set; }
    public UrgencyLevel OriginalUrgency { get; set; }
    public UrgencyLevel? OverriddenUrgency { get; set; }
    public int? OverriddenByUserId { get; set; }
    public User? OverriddenByUser { get; set; }

    [MaxLength(500)]
    public string? OverrideReason { get; set; }

    public bool LowConfidenceFlag { get; set; }
    public bool ReviewRequired { get; set; }
    public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;

    [NotMapped]
    public UrgencyLevel EffectiveUrgency => OverriddenUrgency ?? UrgencyLevel;

    public QueueEntry? QueueEntry { get; set; }
    public EmergencyAlert? EmergencyAlert { get; set; }
}

public sealed class QueueEntry
{
    public long Id { get; set; }
    public int PatientId { get; set; }
    public PatientProfile Patient { get; set; } = null!;
    public long? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public long? AssessmentId { get; set; }
    public TriageAssessment? Assessment { get; set; }
    public DateTimeOffset CheckInTime { get; set; } = DateTimeOffset.UtcNow;
    public decimal PriorityScore { get; set; }
    public UrgencyLevel ConfirmedUrgency { get; set; }
    public QueueStatus QueueStatus { get; set; } = QueueStatus.Waiting;
    public int? HandledByStaffId { get; set; }
    public StaffProfile? HandledByStaff { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Consultation
{
    public long Id { get; set; }
    public int PatientId { get; set; }
    public PatientProfile Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public DoctorProfile Doctor { get; set; } = null!;
    public long? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public required string ClinicalNotes { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlan { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Notification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public long? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public required string Message { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(100)]
    public required string Action { get; set; }

    [MaxLength(80)]
    public string? EntityType { get; set; }

    public long? EntityId { get; set; }
    public string? DetailsJson { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AiModelVersion
{
    public int Id { get; set; }

    [MaxLength(80)]
    public required string VersionName { get; set; }

    [MaxLength(80)]
    public required string Algorithm { get; set; }

    public required string FeatureSchemaJson { get; set; }
    public string? MetricsJson { get; set; }

    [MaxLength(255)]
    public required string FilePath { get; set; }

    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<TriageAssessment> Assessments { get; set; } = new List<TriageAssessment>();
}

public sealed class RedFlagRule
{
    public int Id { get; set; }

    [MaxLength(80)]
    public required string RuleCode { get; set; }

    [MaxLength(150)]
    public required string RuleName { get; set; }

    public required string ConditionJson { get; set; }
    public required string WarningMessage { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(150)]
    public string? ApprovedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EmergencyAlert
{
    public long Id { get; set; }
    public int PatientId { get; set; }
    public PatientProfile Patient { get; set; } = null!;
    public long AssessmentId { get; set; }
    public TriageAssessment Assessment { get; set; } = null!;
    public required string WarningReason { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Created;
    public UserRole AssignedRole { get; set; } = UserRole.Staff;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? AcknowledgedByUserId { get; set; }
    public User? AcknowledgedByUser { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public string? AcknowledgementNotes { get; set; }
    public string? ResolutionNotes { get; set; }
    public int? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
