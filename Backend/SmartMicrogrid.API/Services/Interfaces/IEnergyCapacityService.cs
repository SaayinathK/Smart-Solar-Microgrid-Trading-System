using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IEnergyCapacityService
    {
        Task<CapacityResponseDto?> GetCapacityAsync(string microgridId);
        Task<CapacityResponseDto?> UpdateCapacityAsync(string microgridId, UpdateCapacityDto dto);
    }
}
