// ===========================================================================================================
// File: IReservationRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M2 – Marketplace & Reservation Management
// Section Owned: M2 – Marketplace & Reservation Management
// Author: J. Shathursini (IT23164062)
// Description: Repository interface defining data access contracts for Reservation.
// ===========================================================================================================
using SmartMicrogrid.API.Models.M2;

namespace SmartMicrogrid.API.Repositories.Interfaces;

public interface IReservationRepository
{
    /// <summary>
    /// Creates or registers a new async record.
    /// </summary>
    Task<Reservation> CreateAsync(Reservation reservation);
    /// <summary>
    /// Retrieves by id async details.
    /// </summary>
    Task<Reservation?> GetByIdAsync(string id);
    /// <summary>
    /// Retrieves async details.
    /// </summary>
    Task<List<Reservation>> GetAsync(string? prosumerId, string? status, string? nodeId, int page, int pageSize, IReadOnlyCollection<string>? allowedNodeIds = null);
    /// <summary>
    /// Performs transition async operation.
    /// </summary>
    Task<bool> TransitionAsync(string id, string expectedStatus, string newStatus, string changedBy, string? reason = null);
    /// <summary>
    /// Updates the specified booking async record.
    /// </summary>
    Task<bool> UpdateBookingAsync(Reservation updated, string expectedStatus);
    /// <summary>
    /// Performs count async operation.
    /// </summary>
    Task<long> CountAsync(string? status = null, string? prosumerId = null, IReadOnlyCollection<string>? allowedNodeIds = null);
    /// <summary>
    /// Retrieves approved ended async details.
    /// </summary>
    Task<List<Reservation>> GetApprovedEndedAsync(DateTime now);
    /// <summary>
    /// Performs has active for node async operation.
    /// </summary>
    Task<bool> HasActiveForNodeAsync(string nodeId);
    /// <summary>
    /// Performs mark completed async operation.
    /// </summary>
    Task<bool> MarkCompletedAsync(string id, string changedBy);
    /// <summary>
    /// Performs sum active energy async operation.
    /// </summary>
    Task<double> SumActiveEnergyAsync(string? prosumerId = null, IReadOnlyCollection<string>? allowedNodeIds = null);
    /// <summary>
    /// Performs count completed since async operation.
    /// </summary>
    Task<long> CountCompletedSinceAsync(DateTime since, string? prosumerId = null, IReadOnlyCollection<string>? allowedNodeIds = null);
}
