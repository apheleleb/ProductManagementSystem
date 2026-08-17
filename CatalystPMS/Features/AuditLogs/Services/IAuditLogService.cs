using CatalystPMS.Features.AuditLogs.DTOs;

namespace CatalystPMS.Features.AuditLogs.Services
{
    public interface IAuditLogService
    {
        Task<IEnumerable<AuditLogResponseDto>> GetAllAsync(
            int? productId = null,
            string? actionType = null,
            DateTime? from = null,
            DateTime? to = null);
    }
}
