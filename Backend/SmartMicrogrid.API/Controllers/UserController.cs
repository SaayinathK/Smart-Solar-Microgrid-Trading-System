// ===========================================================================================================
// File: UserController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: API Controller exposing REST endpoints for User management.
// ===========================================================================================================
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        /// <summary>
        /// Initializes a new instance of the UserController class.
        /// </summary>

        public UserController(IUserService userService)
        {
            // Initialize dependencies and state
            _userService = userService;
        }

        /// <summary>
        /// Get currently logged-in user profile
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Execute get current user operations
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResponse("Invalid authorization context."));
            }

            var result = await _userService.GetUserByIdAsync(userId);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Update currently logged-in user profile
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto dto)
        {
            // Execute update current user operations
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResponse("Invalid authorization context."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed.", errors));
            }

            var result = await _userService.UpdateOwnProfileAsync(userId, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Self-service account operations (Prosumer and authenticated users)
        /// </summary>

        /// <summary>
        /// Request and execute self-service account deactivation (Prosumer deactivation flow).
        /// </summary>
        [HttpPost("me/deactivate")]
        public async Task<IActionResult> DeactivateSelf([FromBody] DeactivateRequestDto? dto)
        {
            // Execute self-deactivation operations
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResponse("Invalid authorization context."));
            }

            var result = await _userService.DeactivateSelfAsync(userId, dto?.Reason);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Request deactivation endpoint alias.
        /// </summary>
        [HttpPost("me/request-deactivation")]
        public async Task<IActionResult> RequestDeactivationSelf([FromBody] DeactivateRequestDto? dto)
        {
            // Execute request deactivation alias operations
            return await DeactivateSelf(dto);
        }


        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? search, [FromQuery] Role? role, [FromQuery] bool? activeOnly, [FromQuery] AccountStatus? accountStatus)
        {
            // Execute get all users operations
            var result = await _userService.GetAllUsersAsync(search, role, activeOnly, accountStatus);
            return Ok(result);
        }

        /// <summary>
        /// Get user by ID (Admin or Self)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            // Execute get user by id operations
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            // Only Admin or the user themselves can view user details
            var userRes = await _userService.GetUserByIdAsync(id);
            if (!userRes.Success)
            {
                return NotFound(userRes);
            }

            if (currentUserRole != "Admin" && currentUserId != userRes.Data!.Id && currentUserId != userRes.Data!.Nic)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResponse("Forbidden: You do not have permission to access another user's details."));
            }







            return Ok(userRes);
        }

        /// <summary>
        /// Get prosumer profile by National Identity Card (NIC natural primary key)
        /// </summary>
        [HttpGet("nic/{nic}")]
        public async Task<IActionResult> GetUserByNic(string nic)
        {
            // Execute get user by nic operations
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            var result = await _userService.GetUserByNicAsync(nic);
            if (!result.Success)
            {
                return NotFound(result);
            }

            // Only Admin or the user themselves can view user details
            if (currentUserRole != "Admin" && currentUserId != result.Data!.Id && currentUserId != result.Data!.Nic)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResponse("Forbidden: You do not have permission to access another user's details."));
            }

            return Ok(result);
        }

        /// <summary>
        /// Create a new user with specified role (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            // Execute create user operations
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed.", errors));
            }

            var result = await _userService.CreateUserAsync(dto);
            if (!result.Success)
            {
                return Conflict(result);
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Update user details (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            // Execute update user operations
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed.", errors));
            }

            var result = await _userService.UpdateUserAsync(id, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Activate or Deactivate user (Admin only)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusDto dto)
        {
            // Execute update status operations
            var result = await _userService.UpdateStatusAsync(id, dto.IsActive);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Move a user account through the full lifecycle: Active, Inactive,
        /// Suspended or Pending (Admin only)
        /// </summary>
        [HttpPatch("{id}/account-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAccountStatus(string id, [FromBody] UpdateAccountStatusDto dto)
        {
            // Execute update account status operations
            var result = await _userService.UpdateAccountStatusAsync(id, dto.AccountStatus);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Change user role (Admin only)
        /// </summary>
        [HttpPatch("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleDto dto)
        {
            // Execute update role operations
            var result = await _userService.UpdateRoleAsync(id, dto.Role);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            // Execute delete user operations
            var result = await _userService.DeleteUserAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        /// <summary>
        /// Retrieves current user id details.
        /// </summary>

        private string? GetCurrentUserId()
        {
            // Execute get current user id operations
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        }
        /// <summary>
        /// Retrieves current user role details.
        /// </summary>

        private string? GetCurrentUserRole()
        {
            // Execute get current user role operations
            return User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        }
    }
}
