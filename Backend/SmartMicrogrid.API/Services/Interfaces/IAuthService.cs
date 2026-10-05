// ===========================================================================================================
// File: IAuthService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for Auth operations.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.Auth;
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Creates or registers a new er async record.
        /// </summary>
        Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto dto);
        /// <summary>
        /// Performs login async operation.
        /// </summary>
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto dto);
        /// <summary>
        /// Performs change password async operation.
        /// </summary>
        Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    }
}
