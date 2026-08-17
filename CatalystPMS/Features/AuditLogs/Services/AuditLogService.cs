using CatalystPMS.Features.AuditLogs.DTOs;
using CatalystPMS.Infrastructure.Data;
using Microsoft .EntityFrameworkCore;

namespace CatalystPMS.Features.AuditLogs.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context) => _context = context;

        public async Task<IEnumerable<AuditLogResponseDto>> GetAllAsync(
            int? productId = null,
            string? actionType = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (productId.HasValue)
                query = query.Where(a => a.ProductId == productId.Value);
            if (!string.IsNullOrWhiteSpace(actionType))
                query = query.Where(a => a.ActionType == actionType);
            if (from.HasValue)
                query = query.Where(a => a.LoggedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(a => a.LoggedAt <= to.Value);

            var logs = await query
                .OrderByDescending(a => a.LoggedAt)
                .Take(500) // sane cap for now — add real paging if the log grows large
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
