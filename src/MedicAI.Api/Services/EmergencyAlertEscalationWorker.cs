using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Services;

/// <summary>
/// Prototype acknowledgement watchdog. It escalates unacknowledged red-flag alerts
/// inside the clinic dashboard; it does not contact external emergency services.
/// </summary>
public sealed class EmergencyAlertEscalationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EmergencyAlertEscalationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkSeconds = Math.Max(30, configuration.GetValue<int?>("EmergencyAlerts:CheckIntervalSeconds") ?? 60);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EscalateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Emergency alert escalation check failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(checkSeconds), stoppingToken);
        }
    }

    private async Task EscalateAsync(CancellationToken cancellationToken)
    {
        var thresholdMinutes = Math.Max(1, configuration.GetValue<int?>("EmergencyAlerts:AcknowledgeWithinMinutes") ?? 5);
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-thresholdMinutes);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MedicAiDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var alerts = await db.EmergencyAlerts
            .Include(x => x.Patient)
            .Where(x => x.Status == AlertStatus.Created && x.CreatedAt <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var alert in alerts)
        {
            alert.Status = AlertStatus.Escalated;
            alert.AssignedRole = UserRole.Doctor;
            await db.SaveChangesAsync(cancellationToken);

            await notificationService.CreateForRolesAsync(
                new[] { UserRole.Doctor, UserRole.Admin },
                $"ESCALATED EMERGENCY ALERT: Alert #{alert.Id} for {alert.Patient.FullName} was not acknowledged within {thresholdMinutes} minutes.",
                cancellationToken);

            await auditService.WriteAsync(null, "AUTO_ESCALATE", "EmergencyAlert", alert.Id,
                new { thresholdMinutes, assignedRole = alert.AssignedRole.ToString() }, null, cancellationToken);
        }
    }
}
