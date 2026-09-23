using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IBatteryService
    {
        Task<BatteryResponseDto?> GetBatteryAsync(string microgridId);
        Task<BatteryResponseDto?> UpdateBatteryAsync(string microgridId, UpdateBatteryDto dto);
    }
}
