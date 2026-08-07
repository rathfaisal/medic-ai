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
[Route("api/triage")]
[Authorize]
public sealed class TriageController(
    MedicAiDbContext dbContext,
    ITriageService triageService,
    IAuditService auditService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<TriageAssessmentResponse>> Submit(TriageSubmissionRequest request, CancellationToken cancellationToken)
    {
        if (!request.ConsentConfirmed)
        {
            return BadRequest(new { error = "Consent must be confirmed before submitting symptom information." });
        }

        var userId = User.GetUserId();
        var patient = await dbContext.PatientProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient profile was not found.");

        Appointment? appointment = null;
        if (request.AppointmentId.HasValue)
        {
            appointment = await dbContext.Appointments
                .Include(x => x.SymptomSubmission)
                .FirstOrDefaultAsync(x => x.Id == request.AppointmentId && x.PatientId == patient.Id, cancellationToken)
                ?? throw new KeyNotFoundException("The selected appointment was not found.");

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
            {
                return BadRequest(new { error = "Pre-triage cannot be submitted for this appointment." });
            }
            if (appointment.SymptomSubmission is not null)
            {
                return Conflict(new { error = "A pre-triage submission already exists for this appointment." });
            }
        }

        var submission = new SymptomSubmission
        {
            PatientId = patient.Id,
            Patient = patient,
            AppointmentId = appointment?.Id,
            Appointment = appointment,
            MainSymptom = request.MainSymptom.Trim(),
            DurationValue = request.DurationValue,
            DurationUnit = request.DurationUnit,
            PainSeverity = request.PainSeverity,
            AdditionalSymptoms = request.AdditionalSymptoms?.Trim(),
            ExistingConditionsSnapshot = patient.ExistingConditions,
            ConsentConfirmed = true,
            Fever = request.Fever,
            Vomiting = request.Vomiting,
            Dizziness = request.Dizziness,
            WorseningSymptoms = request.WorseningSymptoms,
            SevereBreathingDifficulty = request.SevereBreathingDifficulty,
            ChestPain = request.ChestPain,
            LossOfConsciousness = request.LossOfConsciousness,
            MajorBleeding = request.MajorBleeding,
            Seizure = request.Seizure,
            SevereAllergicReaction = request.SevereAllergicReaction,
            SuddenSevereWeakness = request.SuddenSevereWeakness,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        dbContext.SymptomSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);

        var assessment = await triageService.AssessAsync(submission, cancellationToken);
        await auditService.WriteAsync(userId, "CREATE", "TriageAssessment", assessment.Id,
            new
            {
                submissionId = submission.Id,
                urgency = assessment.UrgencyLevel.ToString(),
                source = assessment.Source.ToString(),
                assessment.ReviewRequired
            },
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return CreatedAtAction(nameof(GetBySubmission), new { submissionId = submission.Id }, assessment.ToResponse(triageService.GetGuidance(assessment.EffectiveUrgency)));
    }

    [HttpGet("submissions/{submissionId:long}")]
    public async Task<ActionResult<TriageAssessmentResponse>> GetBySubmission(long submissionId, CancellationToken cancellationToken)
    {
        var assessment = await dbContext.TriageAssessments
            .Include(x => x.Submission)
                .ThenInclude(x => x.Patient)
            .FirstOrDefaultAsync(x => x.SubmissionId == submissionId, cancellationToken)
            ?? throw new KeyNotFoundException("Assessment was not found.");

        await EnsureCanViewPatientAsync(assessment.Submission.PatientId, cancellationToken);
        return Ok(assessment.ToResponse(triageService.GetGuidance(assessment.EffectiveUrgency)));
    }

    [HttpGet("appointment/{appointmentId:long}")]
    public async Task<ActionResult<TriageAssessmentResponse>> GetByAppointment(long appointmentId, CancellationToken cancellationToken)
    {
        var assessment = await dbContext.TriageAssessments
            .Include(x => x.Submission)
            .FirstOrDefaultAsync(x => x.Submission.AppointmentId == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Assessment was not found.");

        await EnsureCanViewPatientAsync(assessment.Submission.PatientId, cancellationToken);
        return Ok(assessment.ToResponse(triageService.GetGuidance(assessment.EffectiveUrgency)));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<IReadOnlyList<TriageAssessmentResponse>>> Mine(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var patientId = await dbContext.PatientProfiles.Where(x => x.UserId == userId).Select(x => x.Id).FirstAsync(cancellationToken);
        var assessments = await dbContext.TriageAssessments
            .AsNoTracking()
            .Include(x => x.Submission)
            .Where(x => x.Submission.PatientId == patientId)
            .OrderByDescending(x => x.AssessedAt)
            .ToListAsync(cancellationToken);
        return Ok(assessments.Select(x => x.ToResponse(triageService.GetGuidance(x.EffectiveUrgency))).ToList());
    }

    private async Task EnsureCanViewPatientAsync(int patientId, CancellationToken cancellationToken)
    {
        if (User.GetRoleName() != UserRole.Patient.ToString()) return;
        var ownPatientId = await dbContext.PatientProfiles.Where(x => x.UserId == User.GetUserId()).Select(x => x.Id).FirstAsync(cancellationToken);
        if (ownPatientId != patientId) throw new UnauthorizedAccessException("You cannot view another patient's assessment.");
    }
}
