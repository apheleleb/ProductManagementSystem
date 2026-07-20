using CatalystPMS.Features.AuditLogs.DTOs;
using CatalystPMS.Infrastructure.Data;
using Microsoft .EntityFrameworkCore;

namespace CatalystPMS.Features.AuditLogs.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context) => _context = context;

        public async Task<IEnumerable<AuditLogResponseDto>> GetByProductAsync(int productId)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.ProductId == productId)
                .OrderByDescending(a => a.LoggedAt)
                .ToListAsync();

            return logs.Select(a => new AuditLogResponseDto
            {
                LogId = a.LogId,
                ActionType = a.ActionType,
                FieldName = a.FieldName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ActorUserId = a.ActorUserId,
                LoggedAt = a.LoggedAt
            });
        }
    }
}
