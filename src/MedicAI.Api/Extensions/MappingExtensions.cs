using System.Text.Json;
using MedicAI.Api.Contracts;
using MedicAI.Api.Domain;

namespace MedicAI.Api.Extensions;

public static class MappingExtensions
{
    public static AppointmentResponse ToResponse(this Appointment appointment) => new(
        appointment.Id,
        appointment.PatientId,
        appointment.Patient.FullName,
        appointment.AssignedDoctorId,
        appointment.AssignedDoctor?.FullName,
        appointment.AppointmentDateTime,
        appointment.ServiceType,
        appointment.Reason,
        appointment.Status,
        appointment.CreatedAt,
        appointment.SymptomSubmission is not null,
        appointment.QueueEntry is
        {
            QueueStatus: QueueStatus.Waiting or QueueStatus.Called or QueueStatus.InConsultation
        });

    public static TriageAssessmentResponse ToResponse(this TriageAssessment assessment, string guidance)
    {
        var flags = string.IsNullOrWhiteSpace(assessment.RuleFlagsJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(assessment.RuleFlagsJson) ?? Array.Empty<string>();

        return new TriageAssessmentResponse(
            assessment.SubmissionId,
            assessment.Id,
            assessment.UrgencyLevel,
            assessment.EffectiveUrgency,
            assessment.Source,
            assessment.ConfidenceScore,
            assessment.LowConfidenceFlag,
            assessment.ReviewRequired,
            flags,
            assessment.Explanation,
            guidance,
            assessment.EffectiveUrgency == UrgencyLevel.EmergencyWarning,
            assessment.AssessedAt,
            assessment.OverriddenUrgency,
            assessment.OverrideReason);
    }

    public static QueueEntryResponse ToResponse(this QueueEntry entry) => new(
        entry.Id,
        entry.PatientId,
        entry.Patient.FullName,
        entry.AppointmentId,
        entry.Appointment?.AppointmentDateTime,
        entry.AssessmentId,
        entry.ConfirmedUrgency,
        entry.PriorityScore,
        entry.QueueStatus,
        entry.CheckInTime,
        entry.Assessment?.ReviewRequired ?? false,
        entry.ConfirmedUrgency == UrgencyLevel.EmergencyWarning);

    public static ConsultationResponse ToResponse(this Consultation consultation) => new(
        consultation.Id,
        consultation.PatientId,
        consultation.Patient.FullName,
        consultation.DoctorId,
        consultation.Doctor.FullName,
        consultation.AppointmentId,
        consultation.ClinicalNotes,
        consultation.Diagnosis,
        consultation.TreatmentPlan,
        consultation.FollowUpDate,
        consultation.CreatedAt);

    public static EmergencyAlertResponse ToResponse(this EmergencyAlert alert) => new(
        alert.Id,
        alert.PatientId,
        alert.Patient.FullName,
        alert.AssessmentId,
        alert.WarningReason,
        alert.Status,
        alert.AssignedRole,
        alert.CreatedAt,
        GetNullableDisplayName(alert.AcknowledgedByUser),
        alert.AcknowledgedAt,
        alert.AcknowledgementNotes,
        alert.ResolutionNotes,
        GetNullableDisplayName(alert.ResolvedByUser),
        alert.ResolvedAt);

    public static string GetDisplayName(this User user) => GetNullableDisplayName(user) ?? user.Email;

    private static string? GetNullableDisplayName(User? user) =>
        user?.PatientProfile?.FullName
        ?? user?.DoctorProfile?.FullName
        ?? user?.StaffProfile?.FullName
        ?? user?.Email;
}
