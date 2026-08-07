using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.Extensions;
using MedicAI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/doctor")]
[Authorize]
public sealed class DoctorController(
    MedicAiDbContext dbContext,
    ITriageService triageService,
    IQueueService queueService,
    IAuditService auditService,
    INotificationService notificationService) : ControllerBase
{
    [HttpGet("patients/{patientId:int}/review")]
    [Authorize(Roles = "Staff,Doctor,Admin")]
    public async Task<ActionResult<PatientReviewResponse>> ReviewPatient(int patientId, CancellationToken cancellationToken)
    {
        var patient = await dbContext.PatientProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient was not found.");

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.AssignedDoctor)
            .Include(x => x.SymptomSubmission)
            .Include(x => x.QueueEntry)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.AppointmentDateTime)
            .Take(20)
            .ToListAsync(cancellationToken);

        var assessments = await dbContext.TriageAssessments
            .AsNoTracking()
            .Include(x => x.Submission)
            .Where(x => x.Submission.PatientId == patientId)
            .OrderByDescending(x => x.AssessedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var consultations = new List<Consultation>();
        if (User.IsInRole(UserRole.Doctor.ToString()) || User.IsInRole(UserRole.Admin.ToString()))
        {
            consultations = await dbContext.Consultations
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .Where(x => x.PatientId == patientId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);
        }

        await auditService.WriteAsync(User.GetUserId(), "VIEW", "PatientProfile", patientId,
            new { purpose = "patient-review" }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        var profileResponse = new PatientProfileResponse(
            patient.Id, patient.UserId, patient.User.Email, patient.FullName, patient.DateOfBirth,
            patient.Gender, patient.Phone, patient.Address, patient.Allergies, patient.ExistingConditions, patient.EmergencyContact);

        return Ok(new PatientReviewResponse(
            profileResponse,
            appointments.Select(x => x.ToResponse()).ToList(),
            assessments.Select(x => x.ToResponse(triageService.GetGuidance(x.EffectiveUrgency))).ToList(),
            consultations.Select(x => x.ToResponse()).ToList()));
    }

    [HttpPost("assessments/{assessmentId:long}/override")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<TriageAssessmentResponse>> OverrideUrgency(
        long assessmentId,
        OverrideUrgencyRequest request,
        CancellationToken cancellationToken)
    {
        var assessment = await dbContext.TriageAssessments
            .Include(x => x.Submission)
            .FirstOrDefaultAsync(x => x.Id == assessmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Assessment was not found.");

        assessment.OverriddenUrgency = request.Urgency;
        assessment.OverriddenByUserId = User.GetUserId();
        assessment.OverrideReason = request.Reason.Trim();
        assessment.ReviewRequired = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Urgency == UrgencyLevel.EmergencyWarning &&
            !await dbContext.EmergencyAlerts.AnyAsync(x => x.AssessmentId == assessment.Id, cancellationToken))
        {
            var patient = await dbContext.PatientProfiles.FirstAsync(x => x.Id == assessment.Submission.PatientId, cancellationToken);
            dbContext.EmergencyAlerts.Add(new EmergencyAlert
            {
                PatientId = patient.Id,
                Patient = patient,
                AssessmentId = assessment.Id,
                Assessment = assessment,
                WarningReason = $"Doctor escalated assessment to emergency warning: {request.Reason.Trim()}",
                Status = AlertStatus.Created,
                AssignedRole = UserRole.Staff
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await notificationService.CreateForRolesAsync(
                new[] { UserRole.Staff, UserRole.Doctor, UserRole.Admin },
                $"EMERGENCY WARNING: A doctor escalated assessment #{assessment.Id}. Immediate action is required.",
                cancellationToken);
        }

        await queueService.RecalculateForAssessmentAsync(assessment.Id, cancellationToken);

        await auditService.WriteAsync(User.GetUserId(), "OVERRIDE", "TriageAssessment", assessment.Id,
            new
            {
                original = assessment.UrgencyLevel.ToString(),
                revised = request.Urgency.ToString(),
                reason = request.Reason
            }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return Ok(assessment.ToResponse(triageService.GetGuidance(assessment.EffectiveUrgency)));
    }

    [HttpPost("consultations")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ConsultationResponse>> RecordConsultation(ConsultationRequest request, CancellationToken cancellationToken)
    {
        var doctor = await dbContext.DoctorProfiles
            .FirstOrDefaultAsync(x => x.UserId == User.GetUserId(), cancellationToken)
            ?? throw new KeyNotFoundException("Doctor profile was not found.");
        var patient = await dbContext.PatientProfiles.FindAsync(new object[] { request.PatientId }, cancellationToken)
            ?? throw new KeyNotFoundException("Patient was not found.");

        Appointment? appointment = null;
        if (request.AppointmentId.HasValue)
        {
            appointment = await dbContext.Appointments
                .Include(x => x.Consultation)
                .Include(x => x.QueueEntry)
                .FirstOrDefaultAsync(x => x.Id == request.AppointmentId && x.PatientId == request.PatientId, cancellationToken)
                ?? throw new KeyNotFoundException("Appointment was not found.");
            if (appointment.Consultation is not null) return Conflict(new { error = "A consultation has already been recorded for this appointment." });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var consultation = new Consultation
        {
            PatientId = patient.Id,
            Patient = patient,
            DoctorId = doctor.Id,
            Doctor = doctor,
            AppointmentId = appointment?.Id,
            Appointment = appointment,
            ClinicalNotes = request.ClinicalNotes.Trim(),
            Diagnosis = request.Diagnosis?.Trim(),
            TreatmentPlan = request.TreatmentPlan?.Trim(),
            FollowUpDate = request.FollowUpDate,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Consultations.Add(consultation);

        if (appointment is not null)
        {
            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;
            if (appointment.QueueEntry is not null)
            {
                appointment.QueueEntry.QueueStatus = QueueStatus.Completed;
                appointment.QueueEntry.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await auditService.WriteAsync(User.GetUserId(), "CREATE", "Consultation", consultation.Id,
            new { consultation.PatientId, consultation.AppointmentId }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return CreatedAtAction(nameof(ReviewPatient), new { patientId = patient.Id }, consultation.ToResponse());
    }
}
