using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(MedicAiDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var notifications = await dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new NotificationResponse(x.Id, x.Message, x.Channel, x.DeliveryStatus, x.IsRead, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(notifications);
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var notification = await dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Notification was not found.");
        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
