using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null)
        {
            var users = await _userRepository.GetAllAsync(searchTerm, roleFilter, activeOnly);
            var dtos = users.Select(MapToUserResponseDto);
            return ApiResponse<IEnumerable<UserResponseDto>>.SuccessResponse(dtos, "Users retrieved successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user));
        }

        public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();

            if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("An account with this email address already exists.");
            }

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                Role = dto.Role,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);
            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(createdUser), "User created successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> UpdateUserAsync(string id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;

            if (dto.Role.HasValue)
            {
                user.Role = dto.Role.Value;
            }

            if (dto.IsActive.HasValue)
            {
                user.IsActive = dto.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to update user.");
            }

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user), "User updated successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> UpdateOwnProfileAsync(string userId, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User profile not found.");
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to update profile.");
            }

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user), "Profile updated successfully.");
        }

        public async Task<ApiResponse<UserResponseDto>> UpdateStatusAsync(string id, bool isActive)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to update user status.");
            }

            var statusMsg = isActive ? "User account activated successfully." : "User account deactivated successfully.";
            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user), statusMsg);
        }

        public async Task<ApiResponse<UserResponseDto>> UpdateRoleAsync(string id, Role newRole)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            user.Role = newRole;
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to update user role.");
            }

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user), $"Role updated to {newRole}.");
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User not found.");
            }

            var deleted = await _userRepository.DeleteAsync(id);
            if (!deleted)
            {
                return ApiResponse<bool>.FailureResponse("Failed to delete user.");
            }

            return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully.");
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
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
