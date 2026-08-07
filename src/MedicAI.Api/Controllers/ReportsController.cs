using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Doctor,Admin")]
public sealed class ReportsController(MedicAiDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> Summary(CancellationToken cancellationToken)
    {
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var tomorrow = today.AddDays(1);
        var last30Days = DateTimeOffset.UtcNow.AddDays(-30);

        var todayAppointments = await dbContext.Appointments.CountAsync(x => x.AppointmentDateTime >= today && x.AppointmentDateTime < tomorrow, cancellationToken);
        var waitingPatients = await dbContext.QueueEntries.CountAsync(x => x.QueueStatus == QueueStatus.Waiting || x.QueueStatus == QueueStatus.Called, cancellationToken);
        var emergencyAlerts = await dbContext.EmergencyAlerts.CountAsync(x => x.Status != AlertStatus.Resolved && x.Status != AlertStatus.CancelledAsFalseAlarm, cancellationToken);
        var totalPatients = await dbContext.PatientProfiles.CountAsync(cancellationToken);
        var overrides = await dbContext.TriageAssessments.CountAsync(x => x.OverriddenUrgency != null && x.AssessedAt >= last30Days, cancellationToken);
        var consultations = await dbContext.Consultations.CountAsync(x => x.CreatedAt >= last30Days, cancellationToken);

        var triage = await dbContext.TriageAssessments
            .GroupBy(x => x.OverriddenUrgency ?? x.UrgencyLevel)
            .Select(x => new { Urgency = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return Ok(new DashboardSummaryResponse(
            todayAppointments,
            waitingPatients,
            emergencyAlerts,
            totalPatients,
            triage.ToDictionary(x => x.Urgency.ToString(), x => x.Count),
            overrides,
            consultations));
    }

    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> AuditLogs(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);
        var query = dbContext.AuditLogs.AsNoTracking().Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);

        var logs = await query.OrderByDescending(x => x.CreatedAt).Take(take)
            .Select(x => new AuditLogResponse(
                x.Id,
                x.UserId,
                x.User != null ? x.User.Email : null,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.DetailsJson,
                x.IpAddress,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(logs);
    }
}
