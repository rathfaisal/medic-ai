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
[Route("api/emergency-alerts")]
[Authorize(Roles = "Staff,Doctor,Admin")]
public sealed class EmergencyAlertsController(MedicAiDbContext dbContext, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmergencyAlertResponse>>> Get(
        [FromQuery] bool includeResolved = false,
        [FromQuery] AlertStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmergencyAlerts
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.AcknowledgedByUser!).ThenInclude(x => x.PatientProfile)
            .Include(x => x.AcknowledgedByUser!).ThenInclude(x => x.StaffProfile)
            .Include(x => x.AcknowledgedByUser!).ThenInclude(x => x.DoctorProfile)
            .Include(x => x.ResolvedByUser!).ThenInclude(x => x.PatientProfile)
            .Include(x => x.ResolvedByUser!).ThenInclude(x => x.StaffProfile)
            .Include(x => x.ResolvedByUser!).ThenInclude(x => x.DoctorProfile)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }
        else if (!includeResolved)
        {
            query = query.Where(x => x.Status != AlertStatus.Resolved && x.Status != AlertStatus.CancelledAsFalseAlarm);
        }

        var alerts = await query.OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(cancellationToken);
        return Ok(alerts.Select(x => x.ToResponse()).ToList());
    }

    [HttpPost("{id:long}/acknowledge")]
    public async Task<ActionResult<EmergencyAlertResponse>> Acknowledge(long id, EmergencyAlertActionRequest request, CancellationToken cancellationToken)
    {
        var alert = await LoadAsync(id, cancellationToken);
        if (alert.Status is AlertStatus.Resolved or AlertStatus.CancelledAsFalseAlarm)
            return BadRequest(new { error = "A resolved alert cannot be acknowledged again." });

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedByUserId = User.GetUserId();
        alert.AcknowledgedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes)) alert.AcknowledgementNotes = request.Notes.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "ACKNOWLEDGE", "EmergencyAlert", alert.Id,
            new { request.Notes }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok((await LoadAsync(id, cancellationToken)).ToResponse());
    }

    [HttpPost("{id:long}/resolve")]
    public async Task<ActionResult<EmergencyAlertResponse>> Resolve(long id, EmergencyAlertActionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Notes))
            return BadRequest(new { error = "Resolution notes are required." });

        var alert = await LoadAsync(id, cancellationToken);
        alert.Status = AlertStatus.Resolved;
        alert.ResolvedByUserId = User.GetUserId();
        alert.ResolvedAt = DateTimeOffset.UtcNow;
        alert.ResolutionNotes = request.Notes.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "RESOLVE", "EmergencyAlert", alert.Id,
            new { request.Notes }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok((await LoadAsync(id, cancellationToken)).ToResponse());
    }

    [HttpPost("{id:long}/false-alarm")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<EmergencyAlertResponse>> FalseAlarm(long id, EmergencyAlertActionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Notes))
            return BadRequest(new { error = "A clinical or administrative reason is required." });

        var alert = await LoadAsync(id, cancellationToken);
        alert.Status = AlertStatus.CancelledAsFalseAlarm;
        alert.ResolvedByUserId = User.GetUserId();
        alert.ResolvedAt = DateTimeOffset.UtcNow;
        alert.ResolutionNotes = request.Notes.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "CANCEL_FALSE_ALARM", "EmergencyAlert", alert.Id,
            new { request.Notes }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok((await LoadAsync(id, cancellationToken)).ToResponse());
    }

    private async Task<EmergencyAlert> LoadAsync(long id, CancellationToken cancellationToken) =>
        await dbContext.EmergencyAlerts
            .Include(x => x.Patient)
            .Include(x => x.AcknowledgedByUser!).ThenInclude(x => x.PatientProfile)
            .Include(x => x.AcknowledgedByUser!).ThenInclude(x => x.StaffProfile)
            .Include(x => x.AcknowledgedByUser!).ThenInclude(x => x.DoctorProfile)
            .Include(x => x.ResolvedByUser!).ThenInclude(x => x.PatientProfile)
            .Include(x => x.ResolvedByUser!).ThenInclude(x => x.StaffProfile)
            .Include(x => x.ResolvedByUser!).ThenInclude(x => x.DoctorProfile)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new KeyNotFoundException("Emergency alert was not found.");
}
