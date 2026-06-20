using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog auditLog, CancellationToken ct = default);
    }
}
