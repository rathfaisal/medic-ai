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
[Route("api/queue")]
[Authorize]
public sealed class QueueController(IQueueService queueService, MedicAiDbContext dbContext, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Staff,Doctor,Admin")]
    public async Task<ActionResult<IReadOnlyList<QueueEntryResponse>>> Get(CancellationToken cancellationToken)
    {
        var entries = await queueService.GetActiveQueueAsync(cancellationToken);
        return Ok(entries.Select(x => x.ToResponse()).ToList());
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<QueueEntryResponse?>> Mine(CancellationToken cancellationToken)
    {
        var patientId = await dbContext.PatientProfiles
            .Where(x => x.UserId == User.GetUserId())
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        var entry = await dbContext.QueueEntries
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Appointment)
            .Include(x => x.Assessment)
            .Where(x => x.PatientId == patientId && x.QueueStatus != QueueStatus.Removed && x.QueueStatus != QueueStatus.Completed)
            .OrderByDescending(x => x.CheckInTime)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(entry?.ToResponse());
    }

    [HttpPost("check-in")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<QueueEntryResponse>> CheckIn(CheckInRequest request, CancellationToken cancellationToken)
    {
        var entry = await queueService.CheckInAsync(request.AppointmentId, request.ManualUrgency, User.GetUserId(), cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "CHECK_IN", "QueueEntry", entry.Id,
            new { entry.AppointmentId, urgency = entry.ConfirmedUrgency.ToString() },
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(entry.ToResponse());
    }

    [HttpPost("check-in-walk-in")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<QueueEntryResponse>> CheckInWalkIn(WalkInCheckInRequest request, CancellationToken cancellationToken)
    {
        var entry = await queueService.CheckInWalkInAsync(request.PatientId, request.AssessmentId, request.ManualUrgency, User.GetUserId(), cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "CHECK_IN_WALK_IN", "QueueEntry", entry.Id,
            new { entry.PatientId, entry.AssessmentId, urgency = entry.ConfirmedUrgency.ToString() },
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(entry.ToResponse());
    }

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "Staff,Doctor,Admin")]
    public async Task<ActionResult<QueueEntryResponse>> UpdateStatus(long id, QueueStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var entry = await queueService.UpdateStatusAsync(id, request.Status, User.GetUserId(), cancellationToken);
        await dbContext.Entry(entry).Reference(x => x.Patient).LoadAsync(cancellationToken);
        if (entry.AppointmentId.HasValue) await dbContext.Entry(entry).Reference(x => x.Appointment).LoadAsync(cancellationToken);
        if (entry.AssessmentId.HasValue) await dbContext.Entry(entry).Reference(x => x.Assessment).LoadAsync(cancellationToken);

        await auditService.WriteAsync(User.GetUserId(), "UPDATE_STATUS", "QueueEntry", entry.Id,
            new { status = entry.QueueStatus.ToString() }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(entry.ToResponse());
    }

    [HttpPost("call-next")]
    [Authorize(Roles = "Staff,Doctor,Admin")]
    public async Task<ActionResult<QueueEntryResponse?>> CallNext(CancellationToken cancellationToken)
    {
        var entry = await queueService.CallNextAsync(User.GetUserId(), cancellationToken);
        if (entry is null) return Ok(null);

        await auditService.WriteAsync(User.GetUserId(), "CALL_NEXT", "QueueEntry", entry.Id, null, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(entry.ToResponse());
    }
}
