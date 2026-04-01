using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Abstractions;

public interface IAuditRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task<AuditEvent?> GetLatestAsync(Guid notificationId, CancellationToken cancellationToken);
}
