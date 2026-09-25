using SmartMicrogrid.API.DTOs.M2;

namespace SmartMicrogrid.API.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, string actorId, bool staff, string? operatorId = null);
    Task<ReservationResponseDto> UpdateAsync(string id, string? energySlotId, double energyAmount, string actorId, bool staff, string? operatorId = null);
    Task<ReservationResponseDto?> GetByIdAsync(string id, string? operatorId = null);
    Task<List<ReservationResponseDto>> GetAsync(string? prosumerId, string? status, string? nodeId, int page, int pageSize, string? operatorId = null);
    Task<ReservationSummaryDto> SummaryAsync(string? prosumerId = null, string? operatorId = null);
    Task<ReservationResponseDto> TransitionAsync(string id, string action, string actorId, bool staff, string? reason = null, string? operatorId = null);
    Task<ReservationResponseDto> MarkCompletedAsync(string id, string actorId, string? operatorId = null);
    Task<bool> HasActiveForNodeAsync(string nodeId);
    Task<List<string>> GetAssignedMicrogridIdsAsync(string operatorId);
    Task ExpireOverdueAsync(CancellationToken cancellationToken);
}
