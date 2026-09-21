using SmartMicrogrid.API.DTOs.Auth;
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto dto);
        Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    }
}
