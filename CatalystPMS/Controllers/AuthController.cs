using System.Security.Claims;
using CatalystPMS.Features.Auth.DTOs;
using CatalystPMS.Features.Auth.Services;
using CatalystPMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalystPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        /// <summary>Returns the identity Entra ID asserts for the current bearer token.</summary>
        [HttpGet("me")]
        public IActionResult Me()
        {
            // TEMPORARY — remove once this is confirmed working. Dumps every claim
            // ASP.NET Core actually attached to the ClaimsPrincipal for this request.
            Console.WriteLine("─── Claims on User for /api/auth/me ───");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"  {claim.Type} = {claim.Value}");
            }
            Console.WriteLine($"  Identity.IsAuthenticated = {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"  Identity.AuthenticationType = {User.Identity?.AuthenticationType}");

            var userId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fullName = User.FindFirstValue("name") ?? User.Identity?.Name ?? string.Empty;
            var email = User.FindFirstValue("preferred_username")
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty;

            // Read the "roles" claim directly instead of via User.IsInRole(), which
            // depends on RoleClaimType being wired up exactly right internally.
            var assignedRoles = User.Claims
                .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var role = assignedRoles.Contains("ProductManager")
                ? "ProductManager"
                : assignedRoles.Contains("ProductCapturer")
                    ? "ProductCapturer"
                    : string.Empty;

            if (string.IsNullOrEmpty(role))
            {
                return BadRequest(ApiResponse.Fail(
                    "This account has no ProductCapturer or ProductManager role assigned in Entra ID. " +
                    "Ask an administrator to assign one under Enterprise Applications → Users and groups."));
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                userId,
                fullName,
                email,
                role
            }));
        }
    }
}

//    [ApiController]
//    [Route("api/[controller]")]
//    public class AuthController : ControllerBase
//    {
//        private readonly IAuthService _authService;

//        public AuthController(IAuthService authService)
//        {
//            _authService = authService;
//        }

//        /// <summary>Register a new user and assign them a role.</summary>
//        [HttpPost("register")]
//        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
//        {
//            var (success, error, response) = await _authService.RegisterAsync(dto);
//            if (!success) return BadRequest(ApiResponse.Fail(error!));
//            return Ok(ApiResponse<AuthResponseDto>.Ok(response!));
//        }

//        /// <summary>Login and receive a JWT token.</summary>
//        [HttpPost("login")]
//        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
//        {
//            var (success, error, response) = await _authService.LoginAsync(dto);
//            if (!success) return Unauthorized(ApiResponse.Fail(error!));
//            return Ok(ApiResponse<AuthResponseDto>.Ok(response!));
//        }

//}
//}