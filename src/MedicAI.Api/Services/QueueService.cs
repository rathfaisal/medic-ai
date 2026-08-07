using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Services;

public interface IQueueService
{
    Task<QueueEntry> CheckInAsync(long appointmentId, UrgencyLevel? manualUrgency, int staffUserId, CancellationToken cancellationToken = default);
    Task<QueueEntry> CheckInWalkInAsync(int patientId, long? assessmentId, UrgencyLevel? manualUrgency, int staffUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueEntry>> GetActiveQueueAsync(CancellationToken cancellationToken = default);
    Task<QueueEntry> UpdateStatusAsync(long queueEntryId, QueueStatus status, int userId, CancellationToken cancellationToken = default);
    Task<QueueEntry?> CallNextAsync(int userId, CancellationToken cancellationToken = default);
    Task RecalculateForAssessmentAsync(long assessmentId, CancellationToken cancellationToken = default);
}

public sealed class QueueService(MedicAiDbContext dbContext) : IQueueService
{
    public async Task<QueueEntry> CheckInAsync(
        long appointmentId,
        UrgencyLevel? manualUrgency,
        int staffUserId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.Appointments
            .Include(x => x.Patient)
            .Include(x => x.SymptomSubmission!)
                .ThenInclude(x => x.Assessment)
            .Include(x => x.QueueEntry)
            .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment was not found.");

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
        {
            throw new InvalidOperationException("This appointment cannot be checked in.");
        }

        if (appointment.QueueEntry is not null && appointment.QueueEntry.QueueStatus is not QueueStatus.Removed and not QueueStatus.Completed)
        {
            return appointment.QueueEntry;
        }

        var assessment = appointment.SymptomSubmission?.Assessment;
        var urgency = assessment?.EffectiveUrgency ?? manualUrgency
            ?? throw new InvalidOperationException("A manual urgency level is required when no completed pre-triage assessment exists.");

        if (urgency == UrgencyLevel.EmergencyWarning)
        {
            throw new InvalidOperationException("Emergency-warning cases must be handled through the emergency-alert workflow, not the normal queue.");
        }

        var staffProfileId = await dbContext.StaffProfiles
            .Where(x => x.UserId == staffUserId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        QueueEntry entry;
        if (appointment.QueueEntry is not null)
        {
            entry = appointment.QueueEntry;
            entry.AssessmentId = assessment?.Id;
            entry.Assessment = assessment;
            entry.CheckInTime = now;
            entry.ConfirmedUrgency = urgency;
            entry.PriorityScore = CalculatePriorityScore(urgency, now, now);
            entry.QueueStatus = QueueStatus.Waiting;
            entry.HandledByStaffId = staffProfileId;
            entry.UpdatedAt = now;
        }
        else
        {
            entry = new QueueEntry
            {
                PatientId = appointment.PatientId,
                Patient = appointment.Patient,
                AppointmentId = appointment.Id,
                Appointment = appointment,
                AssessmentId = assessment?.Id,
                Assessment = assessment,
                CheckInTime = now,
                ConfirmedUrgency = urgency,
                PriorityScore = CalculatePriorityScore(urgency, now, now),
                QueueStatus = QueueStatus.Waiting,
                HandledByStaffId = staffProfileId,
                UpdatedAt = now
            };
            dbContext.QueueEntries.Add(entry);
        }

        appointment.Status = AppointmentStatus.CheckedIn;
        appointment.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<QueueEntry> CheckInWalkInAsync(
        int patientId,
        long? assessmentId,
        UrgencyLevel? manualUrgency,
        int staffUserId,
        CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.PatientProfiles
            .FirstOrDefaultAsync(x => x.Id == patientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient was not found.");

        var existingActive = await dbContext.QueueEntries.AnyAsync(x =>
            x.PatientId == patientId &&
            x.QueueStatus != QueueStatus.Completed &&
            x.QueueStatus != QueueStatus.Removed,
            cancellationToken);
        if (existingActive) throw new InvalidOperationException("This patient already has an active queue entry.");

        TriageAssessment? assessment = null;
        if (assessmentId.HasValue)
        {
            assessment = await dbContext.TriageAssessments
                .Include(x => x.Submission)
                .FirstOrDefaultAsync(x => x.Id == assessmentId.Value && x.Submission.PatientId == patientId, cancellationToken)
                ?? throw new KeyNotFoundException("The selected assessment was not found for this patient.");
        }
        else
        {
            assessment = await dbContext.TriageAssessments
                .Include(x => x.Submission)
                .Where(x => x.Submission.PatientId == patientId)
                .OrderByDescending(x => x.AssessedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var urgency = assessment?.EffectiveUrgency ?? manualUrgency
            ?? throw new InvalidOperationException("A manual urgency level is required when no pre-triage assessment is selected.");
        if (urgency == UrgencyLevel.EmergencyWarning)
            throw new InvalidOperationException("Emergency-warning cases must be handled through the emergency-alert workflow, not the normal queue.");

        var staffProfileId = await dbContext.StaffProfiles
            .Where(x => x.UserId == staffUserId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entry = new QueueEntry
        {
            PatientId = patient.Id,
            Patient = patient,
            AssessmentId = assessment?.Id,
            Assessment = assessment,
            CheckInTime = now,
            ConfirmedUrgency = urgency,
            PriorityScore = CalculatePriorityScore(urgency, now, now),
            QueueStatus = QueueStatus.Waiting,
            HandledByStaffId = staffProfileId,
            UpdatedAt = now
        };
        dbContext.QueueEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<IReadOnlyList<QueueEntry>> GetActiveQueueAsync(CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.QueueEntries
            .Include(x => x.Patient)
            .Include(x => x.Appointment)
            .Include(x => x.Assessment)
            .Where(x => x.QueueStatus == QueueStatus.Waiting || x.QueueStatus == QueueStatus.Called || x.QueueStatus == QueueStatus.InConsultation)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in entries)
        {
            entry.PriorityScore = CalculatePriorityScore(entry.ConfirmedUrgency, entry.CheckInTime, now);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return entries
            .OrderByDescending(x => x.ConfirmedUrgency)
            .ThenByDescending(x => x.PriorityScore)
            .ThenBy(x => x.CheckInTime)
            .ToList();
    }

    public async Task<QueueEntry> UpdateStatusAsync(long queueEntryId, QueueStatus status, int userId, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.QueueEntries
            .Include(x => x.Appointment)
            .FirstOrDefaultAsync(x => x.Id == queueEntryId, cancellationToken)
            ?? throw new KeyNotFoundException("Queue entry was not found.");

        entry.QueueStatus = status;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        if (entry.Appointment is not null)
        {
            entry.Appointment.Status = status switch
            {
                QueueStatus.InConsultation => AppointmentStatus.InConsultation,
                QueueStatus.Completed => AppointmentStatus.Completed,
                _ => entry.Appointment.Status
            };
            entry.Appointment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<QueueEntry?> CallNextAsync(int userId, CancellationToken cancellationToken = default)
    {
        var queue = await GetActiveQueueAsync(cancellationToken);
        var next = queue.FirstOrDefault(x => x.QueueStatus == QueueStatus.Waiting);
        if (next is null) return null;

        next.QueueStatus = QueueStatus.Called;
        next.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return next;
    }

    public async Task RecalculateForAssessmentAsync(long assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await dbContext.TriageAssessments
            .Include(x => x.QueueEntry)
            .FirstOrDefaultAsync(x => x.Id == assessmentId, cancellationToken);

        if (assessment?.QueueEntry is null) return;
        if (assessment.EffectiveUrgency == UrgencyLevel.EmergencyWarning)
        {
            assessment.QueueEntry.QueueStatus = QueueStatus.Removed;
        }
        else
        {
            assessment.QueueEntry.ConfirmedUrgency = assessment.EffectiveUrgency;
            assessment.QueueEntry.PriorityScore = CalculatePriorityScore(
                assessment.EffectiveUrgency,
                assessment.QueueEntry.CheckInTime,
                DateTimeOffset.UtcNow);
        }
        assessment.QueueEntry.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static decimal CalculatePriorityScore(UrgencyLevel urgency, DateTimeOffset checkInTime, DateTimeOffset now)
    {
        var baseScore = urgency switch
        {
            UrgencyLevel.Urgent => 3000m,
            UrgencyLevel.Priority => 2000m,
            UrgencyLevel.Routine => 1000m,
            _ => 0m
        };
        var waitingMinutes = Math.Max(0, (decimal)(now - checkInTime).TotalMinutes);
        return decimal.Round(baseScore + Math.Min(waitingMinutes / 10m, 99m), 2);
    }
}
