using System;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class EnergyCapacityService : IEnergyCapacityService
    {
        private readonly IMicrogridRepository _repository;

        public EnergyCapacityService(IMicrogridRepository repository)
        {
            _repository = repository;
        }

        public async Task<CapacityResponseDto?> GetCapacityAsync(string microgridId)
        {
            var microgrid = await _repository.GetByIdAsync(microgridId);
            if (microgrid == null) return null;

            return new CapacityResponseDto
            {
                MicrogridId = microgrid.Id ?? string.Empty,
                TotalCapacity = microgrid.Capacity,
                AvailableCapacity = microgrid.AvailableCapacity,
                ReservedCapacity = microgrid.ReservedCapacity,
                UsedCapacity = microgrid.UsedCapacity
            };
        }

        public async Task<CapacityResponseDto?> UpdateCapacityAsync(string microgridId, UpdateCapacityDto dto)
        {
            var microgrid = await _repository.GetByIdAsync(microgridId);
            if (microgrid == null) return null;

            if (dto.TotalCapacity <= 0)
                throw new ArgumentException("Total capacity must be greater than zero.");

            if (dto.AvailableCapacity < 0 || dto.ReservedCapacity < 0 || dto.UsedCapacity < 0)
                throw new ArgumentException("Capacity allocations cannot be negative.");

            if (dto.AvailableCapacity > dto.TotalCapacity)
                throw new ArgumentException("Available capacity cannot exceed total capacity.");

            if (dto.UsedCapacity + dto.ReservedCapacity > dto.TotalCapacity)
                throw new ArgumentException("Sum of used capacity and reserved capacity cannot exceed total capacity.");

            var success = await _repository.UpdateCapacityAsync(microgridId, dto.TotalCapacity, dto.AvailableCapacity, dto.ReservedCapacity, dto.UsedCapacity);
            if (!success) return null;

            return new CapacityResponseDto
            {
                MicrogridId = microgridId,
                TotalCapacity = dto.TotalCapacity,
                AvailableCapacity = dto.AvailableCapacity,
                ReservedCapacity = dto.ReservedCapacity,
                UsedCapacity = dto.UsedCapacity
            };
        }
    }
}
