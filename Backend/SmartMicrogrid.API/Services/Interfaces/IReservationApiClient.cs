using SmartMicrogrid.API.DTOs.Transactions;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IReservationApiClient
    {
        Task<ReservationDto?> GetReservationAsync(
            string reservationId);
    }
}