// ===========================================================================================================
// File: UserService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Business logic service implementing UserService operations, rules, and workflows.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Helpers;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditService _auditService;
        /// <summary>
        /// Initializes a new instance of the UserService class.
        /// </summary>

        public UserService(IUserRepository userRepository, IAuditService auditService)
        {
            // Initialize dependencies and state
            _userRepository = userRepository;
            _auditService = auditService;
        }
        /// <summary>
        /// Retrieves all users async details.
        /// </summary>

        public async Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null, AccountStatus? accountStatus = null)
        {
            // Execute get all users async operations
            var users = await _userRepository.GetAllAsync(searchTerm, roleFilter, activeOnly, accountStatus);
            var dtos = users.Select(MapToUserResponseDto);
            return ApiResponse<IEnumerable<UserResponseDto>>.SuccessResponse(dtos, "Users retrieved successfully.");
        }
        /// <summary>
        /// Retrieves user by id async details.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(string id)
        {
            // Execute get user by id async operations
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user));
        }
        /// <summary>
        /// Creates or registers a new user async record.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto)
        {
            // Execute create user async operations
            var normalizedEmail = dto.Email.Trim().ToLower();

            if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("An account with this email address already exists.");
            }

            var nic = dto.Nic?.Trim().ToUpper() ?? string.Empty;
            if (dto.Role == Role.Prosumer && string.IsNullOrWhiteSpace(nic))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("National Identity Card (NIC) is required for Prosumer role.");
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
                AccountStatus = dto.IsActive ? AccountStatus.Active : AccountStatus.Inactive,
                IsActive = dto.IsActive,
                StatusChangedAt = DateTime.UtcNow,
                StatusChangedBy = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);

            await _auditService.RecordAsync(
                AuditAction.UserCreated,
                AuditModule.UserManagement,
                $"Account created for {createdUser.Email} with role {createdUser.Role}.",
                userId: createdUser.Id,
                userName: $"{createdUser.FirstName} {createdUser.LastName}",
                role: createdUser.Role.ToString(),
                entityType: nameof(User),
                entityId: createdUser.Id);

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(createdUser), "User created successfully.");
        }
        /// <summary>
        /// Updates the specified user async record.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> UpdateUserAsync(string id, UpdateUserDto dto)
        {
            // Execute update user async operations
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;

            if (dto.Nic != null)
            {
                user.Nic = dto.Nic.Trim().ToUpper();
            }

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

            // The actor is resolved from the current principal by the audit
            // service; entityId identifies the account that was acted on, and the
            // description names it, so the trail answers "who did what to whom".
            await _auditService.RecordAsync(
                AuditAction.UserUpdated,
                AuditModule.UserManagement,
                $"Account details updated for {user.Email} ({user.FirstName} {user.LastName}).",
                entityType: nameof(User),
                entityId: user.Id);

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user), "User updated successfully.");
        }
        /// <summary>
        /// Updates the specified own profile async record.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> UpdateOwnProfileAsync(string userId, UpdateUserDto dto)
        {
            // Execute update own profile async operations
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User profile not found.");
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
            if (dto.Nic != null)
            {
                user.Nic = dto.Nic.Trim().ToUpper();
            }
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to update profile.");
            }

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user), "Profile updated successfully.");
        }
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> UpdateStatusAsync(string id, bool isActive)
        {
            // Execute update status async operations
            return await UpdateAccountStatusAsync(id, isActive ? AccountStatus.Active : AccountStatus.Inactive);
        }
        /// <summary>
        /// Updates the specified account status async record.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> UpdateAccountStatusAsync(string id, AccountStatus newStatus)
        {
            // Execute update account status async operations
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            // M4: keep IsActive (the authentication gate) and AccountStatus (the
            // administrative view) in sync on every transition.
            var previousStatus = M4.DashboardService.ResolveStatus(user);

            if (previousStatus == newStatus)
            {
                return ApiResponse<UserResponseDto>.SuccessResponse(
                    MapToUserResponseDto(user),
                    $"User account is already {newStatus}.");
            }

            // Suspending or deactivating the last remaining active Admin is refused
            // so the platform can never be left without a backoffice officer.
            if (previousStatus == AccountStatus.Active &&
                newStatus != AccountStatus.Active &&
                user.Role == Role.Admin &&
                !await HasOtherActiveAdminAsync(user.Id))
            {
                return ApiResponse<UserResponseDto>.FailureResponse(
                    "The last active Backoffice officer account cannot be suspended or deactivated.");
            }

            var isActive = newStatus == AccountStatus.Active;

            user.IsActive = isActive;
            user.AccountStatus = newStatus;
            user.StatusChangedAt = DateTime.UtcNow;
            user.StatusChangedBy = "Admin";
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to update user status.");
            }

            var action = ResolveStatusAction(previousStatus, newStatus);

            await _auditService.RecordAsync(
                action,
                AuditModule.UserManagement,
                $"Account status changed from {previousStatus} to {newStatus} for {user.Email} ({user.FirstName} {user.LastName}).",
                entityType: nameof(User),
                entityId: user.Id);

            return ApiResponse<UserResponseDto>.SuccessResponse(
                MapToUserResponseDto(user),
                $"User account status changed to {newStatus}.");
        }

        /// <summary>
        /// True when at least one Admin other than <paramref name="excludedUserId"/>
        /// is currently active, i.e. the platform still has a backoffice officer
        /// if <paramref name="excludedUserId"/> is stood down.
        /// </summary>
        /// <summary>
        /// Deactivates the authenticated user's own account (self-service Prosumer deactivation).
        /// </summary>
        public async Task<ApiResponse<UserResponseDto>> DeactivateSelfAsync(string userId, string? reason = null)
        {
            // Execute self-deactivation async operations
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            var previousStatus = M4.DashboardService.ResolveStatus(user);
            if (previousStatus == AccountStatus.Inactive)
            {
                return ApiResponse<UserResponseDto>.SuccessResponse(
                    MapToUserResponseDto(user),
                    "Your account is already deactivated.");
            }

            // Suspending or deactivating the last remaining active Admin is refused
            if (user.Role == Role.Admin && !await HasOtherActiveAdminAsync(user.Id))
            {
                return ApiResponse<UserResponseDto>.FailureResponse(
                    "The last active Backoffice officer account cannot be deactivated.");
            }

            user.IsActive = false;
            user.AccountStatus = AccountStatus.Inactive;
            user.StatusChangedAt = DateTime.UtcNow;
            user.StatusChangedBy = $"{user.FirstName} {user.LastName} (Self)";
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to deactivate account.");
            }

            await _auditService.RecordAsync(
                AuditAction.UserDeactivated,
                AuditModule.UserManagement,
                $"User {user.Email} ({user.FirstName} {user.LastName}) requested and confirmed account deactivation. Reason: {reason ?? "Self-service deactivation"}",
                userId: user.Id,
                userName: $"{user.FirstName} {user.LastName}",
                role: user.Role.ToString(),
                entityType: nameof(User),
                entityId: user.Id);

            return ApiResponse<UserResponseDto>.SuccessResponse(
                MapToUserResponseDto(user),
                "Your account has been deactivated successfully. You can contact an administrator if you wish to reactivate in the future.");
        }

        private async Task<bool> HasOtherActiveAdminAsync(string excludedUserId)
        {
            // Execute has other active admin async operations
            var activeAdmins = await _userRepository.GetAllAsync(null, Role.Admin, true);
            return activeAdmins.Any(a => a.Id != excludedUserId);
        }
        /// <summary>
        /// Performs resolve status action operation.
        /// </summary>

        private static string ResolveStatusAction(AccountStatus previous, AccountStatus current)
        {
            // Execute resolve status action operations
            if (current == AccountStatus.Active)
            {
                return previous == AccountStatus.Suspended
                    ? AuditAction.UserReactivated
                    : AuditAction.UserActivated;
            }

            if (current == AccountStatus.Suspended)
                return AuditAction.UserSuspended;

            if (current == AccountStatus.Pending)
                return AuditAction.UserStatusChanged;

            return previous == AccountStatus.Suspended
                ? AuditAction.UserDeactivated
                : AuditAction.UserStatusChanged;
        }
        /// <summary>
        /// Updates the specified role async record.
        /// </summary>

        public async Task<ApiResponse<UserResponseDto>> UpdateRoleAsync(string id, Role newRole)
        {
            // Execute update role async operations
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("User not found.");
            }

            var previousRole = user.Role;

            // Losing Admin rights is as final as being stood down: only an Admin may
            // reassign roles, so demoting the last one would leave nobody able to
            // promote anybody back. Apply the same guard as the status transition.
            if (previousRole == Role.Admin && newRole != Role.Admin && !await HasOtherActiveAdminAsync(user.Id))
            {
                return ApiResponse<UserResponseDto>.FailureResponse(
                    "The last active Backoffice officer account cannot have its role changed.");
            }

            user.Role = newRole;
            user.UpdatedAt = DateTime.UtcNow;

            var updated = await _userRepository.UpdateAsync(user);
            if (!updated)
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Failed to update user role.");
            }

            // M4: a re-assignment of an existing role is ROLE_CHANGED, a first
            // assignment at creation time is ROLE_ASSIGNED.
            await _auditService.RecordAsync(
                AuditAction.RoleChanged,
                AuditModule.UserManagement,
                $"Role changed from {previousRole} to {newRole} for {user.Email} ({user.FirstName} {user.LastName}).",
                entityType: nameof(User),
                entityId: user.Id);

            return ApiResponse<UserResponseDto>.SuccessResponse(MapToUserResponseDto(user), $"Role updated to {newRole}.");
        }
        /// <summary>
        /// Deletes or removes the designated user async record.
        /// </summary>

        public async Task<ApiResponse<bool>> DeleteUserAsync(string id)
        {
            // Execute delete user async operations
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User not found.");
            }

            // Deleting an administrator is irreversible and removes the only role
            // that can restore administration, so the last one is protected here too.
            if (user.Role == Role.Admin && !await HasOtherActiveAdminAsync(user.Id))
            {
                return ApiResponse<bool>.FailureResponse(
                    "The last active Backoffice officer account cannot be deleted.");
            }

            var deleted = await _userRepository.DeleteAsync(id);
            if (!deleted)
            {
                return ApiResponse<bool>.FailureResponse("Failed to delete user.");
            }

            await _auditService.RecordAsync(
                AuditAction.UserDeleted,
                AuditModule.UserManagement,
                $"Account deleted for {user.Email} ({user.FirstName} {user.LastName}, role {user.Role}).",
                entityType: nameof(User),
                entityId: user.Id);

            return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully.");
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
