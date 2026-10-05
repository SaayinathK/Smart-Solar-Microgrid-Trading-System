// ===========================================================================================================
// File: IReservationService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M2 – Marketplace & Reservation Management
// Section Owned: M2 – Marketplace & Reservation Management
// Author: J. Shathursini (IT23164062)
// Description: Service interface defining contract for Reservation operations.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.M2;

namespace SmartMicrogrid.API.Services.Interfaces;

public interface IReservationService
{
    /// <summary>
    /// Creates or registers a new async record.
    /// </summary>
    Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, string actorId, bool staff, string? operatorId = null);
    /// <summary>
    /// Updates the specified async record.
    /// </summary>
    Task<ReservationResponseDto> UpdateAsync(string id, string? energySlotId, double energyAmount, string actorId, bool staff, string? operatorId = null, DateTime? newStartTime = null);
    /// <summary>
    /// Retrieves by id async details.
    /// </summary>
    Task<ReservationResponseDto?> GetByIdAsync(string id, string? operatorId = null);
    /// <summary>
    /// Retrieves async details.
    /// </summary>
    Task<List<ReservationResponseDto>> GetAsync(string? prosumerId, string? status, string? nodeId, int page, int pageSize, string? operatorId = null);
    /// <summary>
    /// Performs summary async operation.
    /// </summary>
    Task<ReservationSummaryDto> SummaryAsync(string? prosumerId = null, string? operatorId = null);
    /// <summary>
    /// Performs transition async operation.
    /// </summary>
    Task<ReservationResponseDto> TransitionAsync(string id, string action, string actorId, bool staff, string? reason = null, string? operatorId = null);
    /// <summary>
    /// Performs mark completed async operation.
    /// </summary>
    Task<ReservationResponseDto> MarkCompletedAsync(string id, string actorId, string? operatorId = null);
    /// <summary>
    /// Performs has active for node async operation.
    /// </summary>
    Task<bool> HasActiveForNodeAsync(string nodeId);
    /// <summary>
    /// Retrieves assigned microgrid ids async details.
    /// </summary>
    Task<List<string>> GetAssignedMicrogridIdsAsync(string operatorId);
    /// <summary>
    /// Performs expire overdue async operation.
    /// </summary>
    Task ExpireOverdueAsync(CancellationToken cancellationToken);
}
