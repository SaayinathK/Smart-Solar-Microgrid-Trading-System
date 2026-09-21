using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null);
        Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(string id);
        Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto);
        Task<ApiResponse<UserResponseDto>> UpdateUserAsync(string id, UpdateUserDto dto);
        Task<ApiResponse<UserResponseDto>> UpdateOwnProfileAsync(string userId, UpdateUserDto dto);
        Task<ApiResponse<UserResponseDto>> UpdateStatusAsync(string id, bool isActive);
        Task<ApiResponse<UserResponseDto>> UpdateRoleAsync(string id, Role newRole);
        Task<ApiResponse<bool>> DeleteUserAsync(string id);
    }
}
