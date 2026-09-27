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
    private bool IsAdmin => User.IsInRole("Admin");
    private bool IsOperator => User.IsInRole("MicrogridOperator");
    private bool IsStaff => IsAdmin || IsOperator;
    private async Task<string?> CurrentNicAsync() => (await _users.GetByIdAsync(ActorId))?.Nic;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] string? nodeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (IsAdmin)
        {
            var rows = await _service.GetAsync(null, status, nodeId, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), null);
            return Ok(ApiResponse<object>.SuccessResponse(rows, "Reservations retrieved."));
        }
        if (IsOperator)
        {
            var rows = await _service.GetAsync(null, status, nodeId, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), ActorId);
            return Ok(ApiResponse<object>.SuccessResponse(rows, "Microgrid reservations retrieved."));
        }
        var nic = await CurrentNicAsync();
        if (string.IsNullOrWhiteSpace(nic)) return Unauthorized(ApiResponse<object>.FailureResponse("The prosumer profile has no NIC. Contact an Admin to complete the profile."));
        var prosumerRows = await _service.GetAsync(nic, status, nodeId, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), null);
        return Ok(ApiResponse<object>.SuccessResponse(prosumerRows, "Reservations retrieved."));
    }
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? nic = null)
    {
        if (IsAdmin)
        {
            return Ok(ApiResponse<object>.SuccessResponse(await _service.SummaryAsync(nic?.Trim().ToUpperInvariant(), null)));
        }
        if (IsOperator)
        {
            return Ok(ApiResponse<object>.SuccessResponse(await _service.SummaryAsync(null, ActorId)));
        }
        var targetNic = await CurrentNicAsync();
        if (string.IsNullOrWhiteSpace(targetNic)) return Unauthorized(ApiResponse<object>.FailureResponse("The prosumer profile has no NIC."));
        return Ok(ApiResponse<object>.SuccessResponse(await _service.SummaryAsync(targetNic.Trim().ToUpperInvariant(), null)));
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var row = await _service.GetByIdAsync(id, IsOperator ? ActorId : null);
        if (row == null) return NotFound(ApiResponse<object>.FailureResponse("Reservation not found."));
        if (!IsAdmin && !IsOperator && row.ProsumerId != await CurrentNicAsync()) return StatusCode(403, ApiResponse<object>.FailureResponse("You can only view your own reservations."));
        return Ok(ApiResponse<object>.SuccessResponse(row));
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        try { var row = await _service.CreateAsync(dto, ActorId, IsStaff, IsOperator ? ActorId : null); return CreatedAtAction(nameof(GetById), new { id = row.Id }, ApiResponse<object>.SuccessResponse(row, "Reservation created and awaiting approval.")); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.FailureResponse(ex.Message)); }
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateReservationDto dto)
    {
        try { return Ok(ApiResponse<object>.SuccessResponse(await _service.UpdateAsync(id, dto.EnergySlotId, dto.EnergyAmount, ActorId, IsStaff, IsOperator ? ActorId : null), "Reservation updated.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.FailureResponse(ex.Message)); }
    }
    [HttpPatch("{id}/approve"), Authorize(Roles="Admin,MicrogridOperator")]
    public Task<IActionResult> Approve(string id) => Change(id, "approve", null);
    [HttpPatch("{id}/reject"), Authorize(Roles="Admin,MicrogridOperator")]
    public Task<IActionResult> Reject(string id, [FromBody] RejectReservationDto dto) => Change(id, "reject", dto.Reason);
    [HttpPatch("{id}/status"), Authorize(Roles="Admin,MicrogridOperator")]
    public Task<IActionResult> UpdateStatus(string id, [FromBody] ReservationStatusUpdateDto dto) =>
        string.Equals(dto.Status, "Approved", StringComparison.OrdinalIgnoreCase) ? Change(id, "approve", null) :
        string.Equals(dto.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ? Change(id, "reject", dto.Reason) :
        Task.FromResult<IActionResult>(BadRequest(ApiResponse<object>.FailureResponse("Status must be Approved or Rejected.")));
    [HttpPatch("{id}/cancel")]
    public Task<IActionResult> Cancel(string id) => Change(id, "cancel", null);
    [HttpPatch("{id}/complete"), Authorize(Roles="Admin,MicrogridOperator")]
    public async Task<IActionResult> Complete(string id)
    {
        try { return Ok(ApiResponse<object>.SuccessResponse(await _service.MarkCompletedAsync(id, ActorId, IsOperator ? ActorId : null), "Reservation completed.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
    }
    [HttpGet("nodes/{nodeId}/has-active"), Authorize(Roles="Admin,MicrogridOperator")]
    public async Task<IActionResult> HasActiveForNode(string nodeId) => Ok(ApiResponse<object>.SuccessResponse(new { hasActiveReservations = await _service.HasActiveForNodeAsync(nodeId) }));
    private async Task<IActionResult> Change(string id, string action, string? reason)
    {
        try { return Ok(ApiResponse<object>.SuccessResponse(await _service.TransitionAsync(id, action, ActorId, IsStaff, reason, IsOperator ? ActorId : null), $"Reservation {action}d.")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.FailureResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.FailureResponse(ex.Message)); }
    }
}
