using CatalystPMS.Core.Enums;
using CatalystPMS.Features.Auth.DTOs;
using CatalystPMS.Infrastructure.Identity;
using CatalystPMS.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtGenerator;

        public AuthService(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtGenerator)
        {
            _userManager = userManager;
            _jwtGenerator = jwtGenerator;
        }

        public async Task<(bool Success, string? Error, AuthResponseDto? Response)> RegisterAsync(RegisterRequestDto dto)
        {
            // Validate role value
            if (dto.Role != UserRoles.ProductCapturer && dto.Role != UserRoles.ProductManager)
                return (false, $"Role must be '{UserRoles.ProductCapturer}' or '{UserRoles.ProductManager}'.", null);

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Username,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors, null);
            }

            await _userManager.AddToRoleAsync(user, dto.Role);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtGenerator.GenerateToken(user, roles);

            return (true, null, new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = dto.Role,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
        }

        public async Task<(bool Success, string? Error, AuthResponseDto? Response)> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return (false, "Invalid username or password.", null);

            if (!user.IsActive)
                return (false, "This account has been deactivated.", null);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtGenerator.GenerateToken(user, roles);

            return (true, null, new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
        }
    }
}
