using SmartMicrogrid.API.DTOs.M2;

namespace SmartMicrogrid.API.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, string actorId, bool staff);
    Task<ReservationResponseDto> UpdateAsync(string id, string? energySlotId, double energyAmount, string actorId, bool staff);
    Task<ReservationResponseDto?> GetByIdAsync(string id);
    Task<List<ReservationResponseDto>> GetAsync(string? prosumerId, string? status, string? nodeId, int page, int pageSize);
    Task<ReservationSummaryDto> SummaryAsync(string? prosumerId = null);
    Task<ReservationResponseDto> TransitionAsync(string id, string action, string actorId, bool staff, string? reason = null);
    Task<ReservationResponseDto> MarkCompletedAsync(string id, string actorId);
    Task<bool> HasActiveForNodeAsync(string nodeId);
    Task ExpireOverdueAsync(CancellationToken cancellationToken);
}
