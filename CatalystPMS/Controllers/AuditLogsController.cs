using CatalystPMS.Features.AuditLogs.DTOs;
using CatalystPMS.Features.AuditLogs.Services;
using CatalystPMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalystPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Returns audit entries system-wide by default, most recent first.
        /// Optional filters narrow the results — none are required.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? productId,
            [FromQuery] string? actionType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var logs = await _auditLogService.GetAllAsync(productId, actionType, from, to);
            return Ok(ApiResponse<IEnumerable<AuditLogResponseDto>>.Ok(logs));
        }
    }
}