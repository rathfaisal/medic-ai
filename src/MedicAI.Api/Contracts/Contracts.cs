using System.ComponentModel.DataAnnotations;
using MedicAI.Api.Domain;

namespace MedicAI.Api.Contracts;

public sealed record RegisterPatientRequest(
    [Required, EmailAddress, MaxLength(191)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MaxLength(150)] string FullName,
    DateOnly DateOfBirth,
    [Required, MaxLength(30)] string Phone,
    [MaxLength(30)] string? Gender = null);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthUserResponse(
    int Id,
    string Email,
    UserRole Role,
    string DisplayName,
    string Token);

public sealed record PatientProfileResponse(
    int PatientId,
    int UserId,
    string Email,
    string FullName,
    DateOnly DateOfBirth,
    string? Gender,
    string Phone,
    string? Address,
    string? Allergies,
    string? ExistingConditions,
    string? EmergencyContact);

public sealed record UpdatePatientProfileRequest(
    [Required, MaxLength(150)] string FullName,
    DateOnly DateOfBirth,
    [MaxLength(30)] string? Gender,
    [Required, MaxLength(30)] string Phone,
    [MaxLength(255)] string? Address,
    string? Allergies,
    string? ExistingConditions,
    [MaxLength(150)] string? EmergencyContact);

public sealed record DoctorSummaryResponse(
    int Id,
    string FullName,
    string RegistrationNo,
    string? Speciality,
    bool IsAvailable);

public sealed record AppointmentRequest(
    int? PatientId,
    int? AssignedDoctorId,
    DateTimeOffset AppointmentDateTime,
    [Required, MaxLength(100)] string ServiceType,
    [MaxLength(500)] string? Reason);

public sealed record AppointmentUpdateRequest(
    int? AssignedDoctorId,
    DateTimeOffset AppointmentDateTime,
    [Required, MaxLength(100)] string ServiceType,
    [MaxLength(500)] string? Reason);

public sealed record AppointmentResponse(
    long Id,
    int PatientId,
    string PatientName,
    int? AssignedDoctorId,
    string? DoctorName,
    DateTimeOffset AppointmentDateTime,
    string ServiceType,
    string? Reason,
    AppointmentStatus Status,
    DateTimeOffset CreatedAt,
    bool HasPreTriage,
    bool IsCheckedIn);

public sealed record TriageSubmissionRequest(
    long? AppointmentId,
    [Required, MaxLength(255)] string MainSymptom,
    [Range(1, 3650)] int DurationValue,
    DurationUnit DurationUnit,
    [Range(0, 10)] short PainSeverity,
    string? AdditionalSymptoms,
    bool ConsentConfirmed,
    bool Fever,
    bool Vomiting,
    bool Dizziness,
    bool WorseningSymptoms,
    bool SevereBreathingDifficulty,
    bool ChestPain,
    bool LossOfConsciousness,
    bool MajorBleeding,
    bool Seizure,
    bool SevereAllergicReaction,
    bool SuddenSevereWeakness);

public sealed record TriageAssessmentResponse(
    long SubmissionId,
    long AssessmentId,
    UrgencyLevel Urgency,
    UrgencyLevel EffectiveUrgency,
    AssessmentSource Source,
    decimal? Confidence,
    bool LowConfidence,
    bool ReviewRequired,
    IReadOnlyList<string> RuleFlags,
    string Explanation,
    string Guidance,
    bool IsEmergency,
    DateTimeOffset AssessedAt,
    UrgencyLevel? OverriddenUrgency,
    string? OverrideReason);

public sealed record CheckInRequest(
    long AppointmentId,
    UrgencyLevel? ManualUrgency);

public sealed record WalkInCheckInRequest(
    int PatientId,
    long? AssessmentId,
    UrgencyLevel? ManualUrgency);

public sealed record QueueStatusUpdateRequest(
    QueueStatus Status);

public sealed record QueueEntryResponse(
    long Id,
    int PatientId,
    string PatientName,
    long? AppointmentId,
    DateTimeOffset? AppointmentDateTime,
    long? AssessmentId,
    UrgencyLevel ConfirmedUrgency,
    decimal PriorityScore,
    QueueStatus QueueStatus,
    DateTimeOffset CheckInTime,
    bool ReviewRequired,
    bool IsEmergency);

public sealed record OverrideUrgencyRequest(
    UrgencyLevel Urgency,
    [Required, MinLength(5), MaxLength(500)] string Reason);

public sealed record ConsultationRequest(
    long? AppointmentId,
    int PatientId,
    [Required, MinLength(5)] string ClinicalNotes,
    string? Diagnosis,
    string? TreatmentPlan,
    DateOnly? FollowUpDate);

public sealed record ConsultationResponse(
    long Id,
    int PatientId,
    string PatientName,
    int DoctorId,
    string DoctorName,
    long? AppointmentId,
    string ClinicalNotes,
    string? Diagnosis,
    string? TreatmentPlan,
    DateOnly? FollowUpDate,
    DateTimeOffset CreatedAt);

public sealed record PatientReviewResponse(
    PatientProfileResponse Patient,
    IReadOnlyList<AppointmentResponse> Appointments,
    IReadOnlyList<TriageAssessmentResponse> Assessments,
    IReadOnlyList<ConsultationResponse> Consultations);

public sealed record AdminCreateUserRequest(
    [Required, EmailAddress, MaxLength(191)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    UserRole Role,
    [Required, MaxLength(150)] string FullName,
    [MaxLength(30)] string? Phone,
    [MaxLength(100)] string? Position,
    [MaxLength(80)] string? RegistrationNo,
    [MaxLength(120)] string? Speciality);

public sealed record AdminUpdateUserRequest(
    UserStatus Status,
    UserRole Role,
    bool? IsDoctorAvailable);

public sealed record UserListResponse(
    int Id,
    string Email,
    string DisplayName,
    UserRole Role,
    UserStatus Status,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    bool? IsDoctorAvailable);

public sealed record EmergencyAlertActionRequest(
    string? Notes);

public sealed record EmergencyAlertResponse(
    long Id,
    int PatientId,
    string PatientName,
    long AssessmentId,
    string WarningReason,
    AlertStatus Status,
    UserRole AssignedRole,
    DateTimeOffset CreatedAt,
    string? AcknowledgedBy,
    DateTimeOffset? AcknowledgedAt,
    string? AcknowledgementNotes,
    string? ResolutionNotes,
    string? ResolvedBy,
    DateTimeOffset? ResolvedAt);

public sealed record NotificationResponse(
    long Id,
    string Message,
    NotificationChannel Channel,
    DeliveryStatus DeliveryStatus,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record AuditLogResponse(
    long Id,
    int? UserId,
    string? UserEmail,
    string Action,
    string? EntityType,
    long? EntityId,
    string? DetailsJson,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public sealed record DashboardSummaryResponse(
    int TodayAppointments,
    int WaitingPatients,
    int EmergencyAlerts,
    int TotalPatients,
    IReadOnlyDictionary<string, int> TriageByUrgency,
    int OverridesLast30Days,
    int ConsultationsLast30Days);
