using CatalystPMS.Features.Auth.DTOs;

namespace CatalystPMS.Features.Auth.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error, AuthResponseDto? Response)> RegisterAsync(RegisterRequestDto dto);
        Task<(bool Success, string? Error, AuthResponseDto? Response)> LoginAsync(LoginRequestDto dto);
    }
}
