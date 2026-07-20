using CatalystPMS.Core.Enums;
using CatalystPMS.Core.Models;
using CatalystPMS.Features.Approvals.DTOs;
using CatalystPMS.Features.Approvals.Services;
using CatalystPMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CatalystPMS.Controllers
{
    /// <summary>
    /// All manager workflow actions on products:
    /// approve, reject, publish, unpublish, archive.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = UserRoles.ProductManager)]
    public class WorkflowController : ControllerBase
    {
        private readonly IApprovalService _approvalService;
        private string CurrentUserId => User.FindFirstValue("sub")!;

        public WorkflowController(IApprovalService approvalService)
        {
            _approvalService = approvalService;
        }

        [HttpPost("{productId:int}/approve")]
        public async Task<IActionResult> Approve(int productId, [FromBody] ApproveProductDto dto)
        {
            var (success, error) = await _approvalService.ApproveAsync(productId, CurrentUserId, dto.Comment);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Product approved."));
        }

        [HttpPost("{productId:int}/reject")]
        public async Task<IActionResult> Reject(int productId, [FromBody] RejectProductDto dto)
        {
            var (success, error) = await _approvalService.RejectAsync(productId, CurrentUserId, dto.Comment);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Product rejected."));
        }

        [HttpPost("{productId:int}/publish")]
        public async Task<IActionResult> Publish(int productId)
        {
            var (success, error) = await _approvalService.PublishAsync(productId, CurrentUserId);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Product published and synced to Data Lake."));
        }

        [HttpPost("{productId:int}/unpublish")]
        public async Task<IActionResult> Unpublish(int productId)
        {
            var (success, error) = await _approvalService.UnpublishAsync(productId, CurrentUserId);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Product unpublished."));
        }

        [HttpPost("{productId:int}/archive")]
        public async Task<IActionResult> Archive(int productId)
        {
            var (success, error) = await _approvalService.ArchiveAsync(productId, CurrentUserId);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Product archived."));
        }

        //restore endpoint to unarchive a product
        [HttpPost("{productId:int}/restore")]
        public async Task<IActionResult> Restore(int productId)
        {
            var (success, error) = await _approvalService.RestoreAsync(productId, CurrentUserId);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Product restored from archive."));
        }
    }
}