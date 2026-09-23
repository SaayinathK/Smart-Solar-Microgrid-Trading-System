using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IEnergySlotService
    {
        Task<IEnumerable<EnergySlotResponseDto>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null);
        Task<EnergySlotResponseDto?> GetByIdAsync(string id);
        Task<EnergySlotResponseDto> CreateAsync(CreateEnergySlotDto dto, string createdBy);
        Task<EnergySlotResponseDto?> UpdateAsync(string id, UpdateEnergySlotDto dto);
        Task<bool> DeleteAsync(string id);
        Task<bool> UpdateStatusAsync(string id, string status);
        Task<IEnumerable<EnergyAvailabilityResponseDto>> GetEnergyAvailabilityAsync(EnergyAvailabilityQueryDto query);
    }
}
