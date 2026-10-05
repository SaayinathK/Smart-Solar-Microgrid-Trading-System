// ===========================================================================================================
// File: AuthService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Business logic service implementing AuthService operations, rules, and workflows.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.Auth;
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;
        private readonly IAuditService _auditService;
        /// <summary>
        /// Initializes a new instance of the AuthService class.
        /// </summary>

        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper, IAuditService auditService)
        {
            // Initialize dependencies and state
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
            _auditService = auditService;
        }
        /// <summary>
        /// Creates or registers a new er async record.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto dto)
        {
            // Execute register async operations
            var normalizedEmail = dto.Email.Trim().ToLower();

            // Admin self-registration is strictly disallowed
            if (dto.Role == Role.Admin)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Self-registration is not allowed for Admin role. Admin accounts must be created by a Backoffice officer.");
            }

            // Prosumer role requires NIC as primary key/identifier
            var nic = dto.Nic?.Trim().ToUpper() ?? string.Empty;
            if (dto.Role == Role.Prosumer && string.IsNullOrWhiteSpace(nic))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("National Identity Card (NIC) is required for Prosumer registration.");
            }

            if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("An account with this email address already exists.");
            }

            if (!string.IsNullOrWhiteSpace(nic) && await _userRepository.ExistsByNicAsync(nic))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("An account with this National Identity Card (NIC) already exists.");
            }

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                Nic = nic,
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                Role = dto.Role,
                IsActive = false,
                AccountStatus = AccountStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);

            var userResponse = MapToUserResponseDto(createdUser);
            return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, $"{user.Role} registration successful. Your account is pending activation by a Backoffice administrator.");
        }
        /// <summary>
        /// Performs login async operation.
        /// </summary>

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto dto)
        {
            // Execute login async operations
            var normalizedEmail = dto.Email.Trim().ToLower();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
            {
                // M4 audit: record the attempt. The email is included so repeated
                // failures against one account are visible, but never the password.
                await _auditService.RecordAsync(
                    AuditAction.LoginFailed,
                    AuditModule.Authentication,
                    user == null
                        ? $"Failed login attempt for unregistered email {normalizedEmail}."
                        : $"Failed login attempt for {normalizedEmail} (incorrect password).",
                    userId: user?.Id,
                    userName: user == null ? normalizedEmail : $"{user.FirstName} {user.LastName}",
                    role: user?.Role.ToString(),
                    entityType: nameof(User),
                    entityId: user?.Id,
                    status: AuditStatus.Failure);

                return ApiResponse<LoginResponseDto>.FailureResponse("Invalid email address or password.");
            }

            if (!user.IsActive)
            {
                await _auditService.RecordAsync(
                    AuditAction.LoginFailed,
                    AuditModule.Authentication,
                    $"Login blocked for {normalizedEmail}: account is {M4.DashboardService.ResolveStatus(user)}.",
                    userId: user.Id,
                    userName: $"{user.FirstName} {user.LastName}",
                    role: user.Role.ToString(),
                    entityType: nameof(User),
                    entityId: user.Id,
                    status: AuditStatus.Failure);

                var resolvedStatus = M4.DashboardService.ResolveStatus(user);
                if (resolvedStatus == AccountStatus.Pending)
                {
                    return ApiResponse<LoginResponseDto>.FailureResponse("Your account is pending activation by a Backoffice administrator. Please wait for approval before logging in.");
                }

                if (resolvedStatus == AccountStatus.Suspended)
                {
                    return ApiResponse<LoginResponseDto>.FailureResponse("Your account has been suspended by an administrator. Please contact support.");
                }

                return ApiResponse<LoginResponseDto>.FailureResponse("Your account has been deactivated. Deactivated accounts can only be reactivated by a Backoffice officer.");
            }

            var (token, expiresAt) = _jwtHelper.GenerateJwtToken(user);
            var userResponse = MapToUserResponseDto(user);

            await _auditService.RecordAsync(
                AuditAction.LoginSuccess,
                AuditModule.Authentication,
                $"{user.FirstName} {user.LastName} signed in successfully.",
                userId: user.Id,
                userName: $"{user.FirstName} {user.LastName}",
                role: user.Role.ToString(),
                entityType: nameof(User),
                entityId: user.Id);

            var loginResponse = new LoginResponseDto
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                User = userResponse
            };

            return ApiResponse<LoginResponseDto>.SuccessResponse(loginResponse, "Login successful.");
        }
        /// <summary>
        /// Performs change password async operation.
        /// </summary>

        public async Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            // Execute change password async operations
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User not found.");
            }

            if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<bool>.FailureResponse("Current password is incorrect.");
            }

            user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<bool>.FailureResponse("Failed to update password.");
            }

            return ApiResponse<bool>.SuccessResponse(true, "Password changed successfully.");
        }
        /// <summary>
        /// Performs map to user response dto operation.
        /// </summary>

        private static UserResponseDto MapToUserResponseDto(User user)
        {
            // Execute map to user response dto operations
            return new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Nic = user.Nic,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                AccountStatus = M4.DashboardService.ResolveStatus(user).ToString(),
                StatusChangedAt = user.StatusChangedAt,
                StatusChangedBy = user.StatusChangedBy,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
