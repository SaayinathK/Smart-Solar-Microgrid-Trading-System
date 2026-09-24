using SmartMicrogrid.API.Models.M2;

namespace SmartMicrogrid.API.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation);
    Task<Reservation?> GetByIdAsync(string id);
    Task<List<Reservation>> GetAsync(string? prosumerId, string? status, string? nodeId, int page, int pageSize);
    Task<bool> TransitionAsync(string id, string expectedStatus, string newStatus, string changedBy, string? reason = null);
    Task<bool> UpdateBookingAsync(Reservation updated, string expectedStatus);
    Task<long> CountAsync(string? status = null, string? prosumerId = null);
    Task<List<Reservation>> GetApprovedEndedAsync(DateTime now);
    Task<bool> HasActiveForNodeAsync(string nodeId);
    Task<bool> MarkCompletedAsync(string id, string changedBy);
    Task<double> SumActiveEnergyAsync(string? prosumerId = null);
    Task<long> CountCompletedSinceAsync(DateTime since, string? prosumerId = null);
}
