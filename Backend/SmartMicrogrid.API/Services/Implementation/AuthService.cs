using SmartMicrogrid.API.DTOs.Auth;
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;

        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
        }

        public async Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto dto)
        {
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
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);

            var userResponse = MapToUserResponseDto(createdUser);
            return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, $"{user.Role} registration successful.");
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return ApiResponse<LoginResponseDto>.FailureResponse("Invalid email address or password.");
            }

            if (!user.IsActive)
            {
                return ApiResponse<LoginResponseDto>.FailureResponse("Your account has been deactivated. Deactivated accounts can only be reactivated by a Backoffice officer.");
            }

            var (token, expiresAt) = _jwtHelper.GenerateJwtToken(user);
            var userResponse = MapToUserResponseDto(user);

            var loginResponse = new LoginResponseDto
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                User = userResponse
            };

            return ApiResponse<LoginResponseDto>.SuccessResponse(loginResponse, "Login successful.");
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
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

        private static UserResponseDto MapToUserResponseDto(User user)
        {
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
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
