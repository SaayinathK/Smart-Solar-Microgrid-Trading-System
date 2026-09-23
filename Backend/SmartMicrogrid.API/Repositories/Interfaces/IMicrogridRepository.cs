using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.Models.M1;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface IMicrogridRepository
    {
        Task<IEnumerable<MicrogridNode>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null);
        Task<MicrogridNode?> GetByIdAsync(string id);
        Task<MicrogridNode> CreateAsync(MicrogridNode microgrid);
        Task<bool> UpdateAsync(string id, MicrogridNode microgrid);
        Task<bool> DeleteAsync(string id);
        Task<bool> UpdateStatusAsync(string id, string status, bool isActive);
        Task<bool> UpdateCapacityAsync(string id, double totalCapacity, double availableCapacity, double reservedCapacity, double usedCapacity);
        Task<bool> UpdateBatteryAsync(string id, double batteryCapacity, double currentBatteryLevel, double batteryPercentage, string batteryStatus);
        Task<long> GetCountAsync(string? status = null);
    }
}
