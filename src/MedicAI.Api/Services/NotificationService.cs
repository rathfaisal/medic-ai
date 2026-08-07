using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Services;

public interface INotificationService
{
    Task CreateForUserAsync(int userId, string message, long? appointmentId = null, CancellationToken cancellationToken = default);
    Task CreateForRolesAsync(IEnumerable<UserRole> roles, string message, CancellationToken cancellationToken = default);
}

public sealed class NotificationService(MedicAiDbContext dbContext) : INotificationService
{
    public async Task CreateForUserAsync(int userId, string message, long? appointmentId = null, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Notification recipient was not found.");

        dbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            User = user,
            AppointmentId = appointmentId,
            Message = message,
            Channel = NotificationChannel.InApp,
            DeliveryStatus = DeliveryStatus.Sent,
            SentAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateForRolesAsync(IEnumerable<UserRole> roles, string message, CancellationToken cancellationToken = default)
    {
        var roleSet = roles.ToHashSet();
        var users = await dbContext.Users
            .Where(x => x.Status == UserStatus.Active && roleSet.Contains(x.Role))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = user.Id,
                User = user,
                Message = message,
                Channel = NotificationChannel.InApp,
                DeliveryStatus = DeliveryStatus.Sent,
                SentAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
