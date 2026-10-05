// ===========================================================================================================
// File: IUserService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for User operations.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Retrieves all users async details.
        /// </summary>
        Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null, AccountStatus? accountStatus = null);
        /// <summary>
        /// Retrieves user by id async details.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(string id);
        /// <summary>
        /// Creates or registers a new user async record.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto);
        /// <summary>
        /// Updates the specified user async record.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> UpdateUserAsync(string id, UpdateUserDto dto);
        /// <summary>
        /// Updates the specified own profile async record.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> UpdateOwnProfileAsync(string userId, UpdateUserDto dto);
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> UpdateStatusAsync(string id, bool isActive);
        /// <summary>
        /// Updates the specified account status async record.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> UpdateAccountStatusAsync(string id, AccountStatus status);
        /// <summary>
        /// Updates the specified role async record.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> UpdateRoleAsync(string id, Role newRole);
        /// <summary>
        /// Deactivates own user account (self-service deactivation for Prosumer/User).
        /// </summary>
        Task<ApiResponse<UserResponseDto>> DeactivateSelfAsync(string userId, string? reason = null);
        /// <summary>
        /// Deletes or removes the designated user async record.
        /// </summary>
        Task<ApiResponse<bool>> DeleteUserAsync(string id);
    }
}
