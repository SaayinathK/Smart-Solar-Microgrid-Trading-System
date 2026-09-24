using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.Models.M1;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface IEnergySlotRepository
    {
        Task<IEnumerable<EnergySlot>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null);
        Task<EnergySlot?> GetByIdAsync(string id);
        Task<EnergySlot> CreateAsync(EnergySlot slot);
        Task<bool> UpdateAsync(string id, EnergySlot slot);
        Task<bool> DeleteAsync(string id);
        Task<bool> UpdateStatusAsync(string id, string status);
        Task<long> GetCountAsync(string? status = null);
        Task<double> GetTotalAvailableEnergyAsync();
    }
}
