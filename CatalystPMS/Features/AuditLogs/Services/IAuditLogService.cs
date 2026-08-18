using CatalystPMS.Features.AuditLogs.DTOs;

namespace CatalystPMS.Features.AuditLogs.Services
{
    public interface IAuditLogService
    {
        Task<IEnumerable<AuditLogResponseDto>> GetByProductAsync(int productId);
    }
}
