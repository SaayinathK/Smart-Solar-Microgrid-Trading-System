using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.M2;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Controllers;

[ApiController, Route("api/reservations"), Authorize]
public class ReservationController : ControllerBase
{
    private readonly IReservationService _service;
    private readonly IUserRepository _users;
    public ReservationController(IReservationService service, IUserRepository users) { _service = service; _users = users; }
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("sub") ?? string.Empty;
    private async Task<Role?> CurrentRoleAsync() => (await _users.GetByIdAsync(ActorId))?.Role;
    private static bool IsBackoffice(Role? role) => role is Role.Admin or Role.Backoffice;
    private static bool IsOperator(Role? role) => role is Role.MicrogridOperator or Role.GridOperator;
    private static bool IsStaff(Role? role) => IsBackoffice(role) || IsOperator(role);
    private static bool IsVerifier(Role? role) => role == Role.TransactionVerifier;
    private static string? OperatorScope(Role? role, string actorId) => IsOperator(role) || IsVerifier(role) ? actorId : null;
    private async Task<string?> CurrentNicAsync() => (await _users.GetByIdAsync(ActorId))?.Nic?.Trim().ToUpperInvariant();

    [HttpGet("access")]
    public async Task<IActionResult> Access()
    {
        var role = await CurrentRoleAsync();
        if (role == null) return Unauthorized(ApiResponse<object>.FailureResponse("The current account could not be identified."));
        var assignmentRole = role switch { Role.Admin or Role.Backoffice => "Backoffice", Role.MicrogridOperator or Role.GridOperator => "GridOperator", _ => role.ToString() };
        var assignedMicrogridIds = IsOperator(role) || IsVerifier(role) ? await _service.GetAssignedMicrogridIdsAsync(ActorId) : new List<string>();
        return Ok(ApiResponse<object>.SuccessResponse(new { role = assignmentRole, canViewReservations = IsStaff(role) || IsVerifier(role) || role == Role.Prosumer, canManageReservations = IsStaff(role), canCreateForProsumer = IsStaff(role), approvedReservationsOnly = IsVerifier(role), microgridScoped = IsOperator(role) || IsVerifier(role), assignedMicrogridIds }));
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] string? nodeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var role = await CurrentRoleAsync();
        if (role == null) return Unauthorized(ApiResponse<object>.FailureResponse("The current account could not be identified."));
        var staff = IsStaff(role); var verifier = IsVerifier(role); var nic = staff || verifier ? null : await CurrentNicAsync();
        if (!staff && !verifier && string.IsNullOrWhiteSpace(nic)) return Unauthorized(ApiResponse<object>.FailureResponse("The prosumer profile has no NIC. Contact Backoffice to complete the profile."));
        var rows = await _service.GetAsync(nic, verifier ? "Approved" : status, nodeId, Math.Max(1,page), Math.Clamp(pageSize,1,100), OperatorScope(role, ActorId));
        return Ok(ApiResponse<object>.SuccessResponse(rows, "Reservations retrieved."));
    }
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? nic = null)
    {
        var role = await CurrentRoleAsync();
        if (role == null) return Unauthorized(ApiResponse<object>.FailureResponse("The current account could not be identified."));
        if (IsVerifier(role)) return Forbid();
        var staff = IsStaff(role); var targetNic = IsBackoffice(role) ? nic : staff ? null : await CurrentNicAsync();
        if (!staff && string.IsNullOrWhiteSpace(targetNic)) return Unauthorized(ApiResponse<object>.FailureResponse("The prosumer profile has no NIC."));
        return Ok(ApiResponse<object>.SuccessResponse(await _service.SummaryAsync(targetNic?.Trim().ToUpperInvariant(), OperatorScope(role, ActorId))));
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var role = await CurrentRoleAsync();
        if (role == null) return Unauthorized(ApiResponse<object>.FailureResponse("The current account could not be identified."));
        ReservationResponseDto? row;
        try { row = await _service.GetByIdAsync(id, OperatorScope(role, ActorId)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        if (row == null) return NotFound(ApiResponse<object>.FailureResponse("Reservation not found."));
        if (IsVerifier(role) && !string.Equals(row.Status, "Approved", StringComparison.OrdinalIgnoreCase)) return Forbid();
        if (!IsStaff(role) && !IsVerifier(role) && !string.Equals(row.ProsumerId, await CurrentNicAsync(), StringComparison.OrdinalIgnoreCase)) return StatusCode(403, ApiResponse<object>.FailureResponse("You can only view your own reservations."));
        return Ok(ApiResponse<object>.SuccessResponse(row));
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var role = await CurrentRoleAsync();
        if (role == null || IsVerifier(role) || !(IsStaff(role) || role == Role.Prosumer)) return Forbid();
        try { var row = await _service.CreateAsync(dto, ActorId, IsStaff(role), OperatorScope(role, ActorId)); return CreatedAtAction(nameof(GetById), new { id = row.Id }, ApiResponse<object>.SuccessResponse(row, "Reservation created and awaiting approval.")); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.FailureResponse(ex.Message)); }
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateReservationDto dto)
    {
        var role = await CurrentRoleAsync();
        if (role == null || IsVerifier(role) || !(IsStaff(role) || role == Role.Prosumer)) return Forbid();
        try { return Ok(ApiResponse<object>.SuccessResponse(await _service.UpdateAsync(id, dto.EnergySlotId, dto.EnergyAmount, ActorId, IsStaff(role), OperatorScope(role, ActorId)), "Reservation updated.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.FailureResponse(ex.Message)); }
    }
    [HttpPatch("{id}/approve"), Authorize(Roles="Backoffice,GridOperator,Admin,MicrogridOperator")]
    public Task<IActionResult> Approve(string id) => Change(id, "approve", null);
    [HttpPatch("{id}/reject"), Authorize(Roles="Backoffice,GridOperator,Admin,MicrogridOperator")]
    public Task<IActionResult> Reject(string id, [FromBody] RejectReservationDto dto) => Change(id, "reject", dto.Reason);
    [HttpPatch("{id}/status"), Authorize(Roles="Backoffice,GridOperator,Admin,MicrogridOperator")]
    public Task<IActionResult> UpdateStatus(string id, [FromBody] ReservationStatusUpdateDto dto) =>
        string.Equals(dto.Status, "Approved", StringComparison.OrdinalIgnoreCase) ? Change(id, "approve", null) :
        string.Equals(dto.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ? Change(id, "reject", dto.Reason) :
        Task.FromResult<IActionResult>(BadRequest(ApiResponse<object>.FailureResponse("Status must be Approved or Rejected.")));
    [HttpPatch("{id}/cancel")]
    public Task<IActionResult> Cancel(string id) => Change(id, "cancel", null);
    [HttpPatch("{id}/complete"), Authorize(Roles="Admin,MicrogridOperator,TransactionVerifier")]
    public async Task<IActionResult> Complete(string id)
    {
        var role = await CurrentRoleAsync();
        if (role == null || !(IsStaff(role) || IsVerifier(role))) return Forbid();
        try { return Ok(ApiResponse<object>.SuccessResponse(await _service.MarkCompletedAsync(id, ActorId, OperatorScope(role, ActorId)), "Reservation completed.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
    }
    [HttpGet("nodes/{nodeId}/has-active"), Authorize(Roles="Backoffice,GridOperator,Admin,MicrogridOperator")]
    public async Task<IActionResult> HasActiveForNode(string nodeId) => Ok(ApiResponse<object>.SuccessResponse(new { hasActiveReservations = await _service.HasActiveForNodeAsync(nodeId) }));
    private async Task<IActionResult> Change(string id, string action, string? reason)
    {
        var role = await CurrentRoleAsync();
        if (role == null || IsVerifier(role) || !(IsStaff(role) || role == Role.Prosumer)) return Forbid();
        if ((action == "approve" || action == "reject") && !IsStaff(role)) return Forbid();
        try { return Ok(ApiResponse<object>.SuccessResponse(await _service.TransitionAsync(id, action, ActorId, IsStaff(role), reason, OperatorScope(role, ActorId)), $"Reservation {action}d.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.FailureResponse(ex.Message)); }
    }
}
