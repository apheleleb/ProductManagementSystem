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

        /// <summary>Get full audit history for a product. Both roles can view.</summary>
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var logs = await _auditLogService.GetByProductAsync(productId);
            return Ok(ApiResponse<IEnumerable<AuditLogResponseDto>>.Ok(logs));
        }
    }
}