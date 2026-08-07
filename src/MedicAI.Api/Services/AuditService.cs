using System.Text.Json;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;

namespace MedicAI.Api.Services;

public interface IAuditService
{
    Task WriteAsync(int? userId, string action, string? entityType = null, long? entityId = null, object? details = null, string? ipAddress = null, CancellationToken cancellationToken = default);
}

public sealed class AuditService(MedicAiDbContext dbContext) : IAuditService
{
    public async Task WriteAsync(
        int? userId,
        string action,
        string? entityType = null,
        long? entityId = null,
        object? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
            IpAddress = ipAddress,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
