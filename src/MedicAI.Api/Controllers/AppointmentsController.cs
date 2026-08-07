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
[Route("api/appointments")]
[Authorize]
public sealed class AppointmentsController(
    MedicAiDbContext dbContext,
    INotificationService notificationService,
    IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> Get(
        [FromQuery] DateOnly? date,
        [FromQuery] AppointmentStatus? status,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var role = User.GetRoleName();
        var query = dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.AssignedDoctor)
            .Include(x => x.SymptomSubmission)
            .Include(x => x.QueueEntry)
            .AsQueryable();

        if (role == UserRole.Patient.ToString())
        {
            var patientId = await GetPatientIdAsync(userId, cancellationToken);
            query = query.Where(x => x.PatientId == patientId);
        }
        else if (role == UserRole.Doctor.ToString())
        {
            var doctorId = await dbContext.DoctorProfiles.Where(x => x.UserId == userId).Select(x => x.Id).FirstAsync(cancellationToken);
            query = query.Where(x => x.AssignedDoctorId == doctorId || x.AssignedDoctorId == null);
        }

        if (date.HasValue)
        {
            var start = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = start.AddDays(1);
            query = query.Where(x => x.AppointmentDateTime >= start && x.AppointmentDateTime < end);
        }
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var appointments = await query.OrderBy(x => x.AppointmentDateTime).ToListAsync(cancellationToken);
        return Ok(appointments.Select(x => x.ToResponse()).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Patient,Staff,Admin")]
    public async Task<ActionResult<AppointmentResponse>> Create(AppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var role = User.GetRoleName();
        var patientId = role == UserRole.Patient.ToString()
            ? await GetPatientIdAsync(userId, cancellationToken)
            : request.PatientId ?? throw new ArgumentException("PatientId is required when staff create an appointment.");

        var appointment = new Appointment
        {
            PatientId = patientId,
            Patient = await dbContext.PatientProfiles.FindAsync(new object[] { patientId }, cancellationToken)
                ?? throw new KeyNotFoundException("Patient was not found."),
            AssignedDoctorId = request.AssignedDoctorId,
            AppointmentDateTime = request.AppointmentDateTime,
            ServiceType = request.ServiceType.Trim(),
            Reason = request.Reason?.Trim(),
            Status = AppointmentStatus.Booked,
            CreatedByUserId = userId,
            CreatedByUser = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken)
                ?? throw new UnauthorizedAccessException(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await ValidateAppointmentAsync(appointment, null, cancellationToken);
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var patientUserId = appointment.Patient.UserId;
        await notificationService.CreateForUserAsync(patientUserId, $"Appointment booked for {appointment.AppointmentDateTime:dd MMM yyyy HH:mm}.", appointment.Id, cancellationToken);
        await auditService.WriteAsync(userId, "CREATE", "Appointment", appointment.Id, new { appointment.PatientId, appointment.AssignedDoctorId, appointment.AppointmentDateTime }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        await dbContext.Entry(appointment).Reference(x => x.AssignedDoctor).LoadAsync(cancellationToken);
        return Created($"/api/appointments/{appointment.Id}", appointment.ToResponse());
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Patient,Staff,Admin")]
    public async Task<ActionResult<AppointmentResponse>> Update(long id, AppointmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var appointment = await LoadAppointmentAsync(id, cancellationToken);
        await EnsureCanManageAsync(appointment, cancellationToken);

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
        {
            return BadRequest(new { error = "This appointment can no longer be changed." });
        }

        appointment.AssignedDoctorId = request.AssignedDoctorId;
        appointment.AppointmentDateTime = request.AppointmentDateTime;
        appointment.ServiceType = request.ServiceType.Trim();
        appointment.Reason = request.Reason?.Trim();
        appointment.UpdatedAt = DateTimeOffset.UtcNow;
        await ValidateAppointmentAsync(appointment, id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.CreateForUserAsync(appointment.Patient.UserId, $"Appointment rescheduled to {appointment.AppointmentDateTime:dd MMM yyyy HH:mm}.", appointment.Id, cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "UPDATE", "Appointment", appointment.Id, new { appointment.AppointmentDateTime }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        appointment.AssignedDoctor = appointment.AssignedDoctorId.HasValue
            ? await dbContext.DoctorProfiles.FindAsync(new object[] { appointment.AssignedDoctorId.Value }, cancellationToken)
            : null;
        return Ok(appointment.ToResponse());
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Patient,Staff,Admin")]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        var appointment = await LoadAppointmentAsync(id, cancellationToken);
        await EnsureCanManageAsync(appointment, cancellationToken);

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return BadRequest(new { error = "A completed appointment cannot be cancelled." });
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = DateTimeOffset.UtcNow;
        if (appointment.QueueEntry is not null) appointment.QueueEntry.QueueStatus = QueueStatus.Removed;
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.CreateForUserAsync(appointment.Patient.UserId, "Your appointment was cancelled.", appointment.Id, cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "CANCEL", "Appointment", appointment.Id, null, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return NoContent();
    }

    private async Task<int> GetPatientIdAsync(int userId, CancellationToken cancellationToken) =>
        await dbContext.PatientProfiles.Where(x => x.UserId == userId).Select(x => x.Id).FirstAsync(cancellationToken);

    private async Task<Appointment> LoadAppointmentAsync(long id, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Include(x => x.Patient)
            .Include(x => x.AssignedDoctor)
            .Include(x => x.SymptomSubmission)
            .Include(x => x.QueueEntry)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new KeyNotFoundException("Appointment was not found.");

    private async Task EnsureCanManageAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        if (User.GetRoleName() != UserRole.Patient.ToString()) return;
        var patientId = await GetPatientIdAsync(User.GetUserId(), cancellationToken);
        if (appointment.PatientId != patientId) throw new UnauthorizedAccessException("You cannot manage another patient's appointment.");
    }

    private async Task ValidateAppointmentAsync(Appointment appointment, long? currentId, CancellationToken cancellationToken)
    {
        if (appointment.AppointmentDateTime <= DateTimeOffset.UtcNow.AddMinutes(15))
        {
            throw new ArgumentException("Appointment time must be at least 15 minutes in the future.");
        }

        if (appointment.AssignedDoctorId.HasValue)
        {
            var doctorAvailable = await dbContext.DoctorProfiles.AnyAsync(
                x => x.Id == appointment.AssignedDoctorId && x.IsAvailable && x.User.Status == UserStatus.Active,
                cancellationToken);
            if (!doctorAvailable) throw new ArgumentException("The selected doctor is unavailable.");

            var doctorConflict = await dbContext.Appointments.AnyAsync(x =>
                x.Id != currentId &&
                x.AssignedDoctorId == appointment.AssignedDoctorId &&
                x.AppointmentDateTime == appointment.AppointmentDateTime &&
                x.Status != AppointmentStatus.Cancelled && x.Status != AppointmentStatus.NoShow,
                cancellationToken);
            if (doctorConflict) throw new ArgumentException("The selected doctor already has an appointment in this time slot.");
        }

        var patientConflict = await dbContext.Appointments.AnyAsync(x =>
            x.Id != currentId &&
            x.PatientId == appointment.PatientId &&
            x.AppointmentDateTime == appointment.AppointmentDateTime &&
            x.Status != AppointmentStatus.Cancelled && x.Status != AppointmentStatus.NoShow,
            cancellationToken);
        if (patientConflict) throw new ArgumentException("The patient already has an appointment in this time slot.");
    }
}
