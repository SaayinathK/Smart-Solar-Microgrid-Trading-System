using SmartMicrogrid.API.DTOs.M4;

namespace SmartMicrogrid.API.Services.Interfaces.M4
{
    /// <summary>
    /// M4 read-only view of M1 (Microgrid &amp; Energy Resource Management).
    /// The dashboard consumes this instead of IMicrogridRepository directly, so a
    /// future change to M1's storage only touches the implementation, not M4.
    /// </summary>
    public interface IMicrogridMonitorService
    {
        Task<ComponentStatDto> GetCountAsync();
        Task<ComponentStatDto> GetCapacitySummaryAsync();
    }

    /// <summary>
    /// M4 read-only view of M2 (Energy Marketplace &amp; Reservation Management).
    /// </summary>
    public interface IReservationMonitorService
    {
        Task<ComponentStatDto> GetCountAsync();
        Task<ComponentStatDto> GetPendingCountAsync();
    }

    /// <summary>
    /// M4 read-only view of M3 (Energy Transaction &amp; Verification Management).
    /// </summary>
    public interface ITransactionMonitorService
    {
        Task<ComponentStatDto> GetCountAsync();
        Task<ComponentStatDto> GetCompletedCountAsync();
    }
}
