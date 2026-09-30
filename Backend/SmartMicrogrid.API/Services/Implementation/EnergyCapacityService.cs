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

            if (!double.IsFinite(dto.TotalCapacity) || !double.IsFinite(dto.AvailableCapacity) || !double.IsFinite(dto.ReservedCapacity) || !double.IsFinite(dto.UsedCapacity))
                throw new ArgumentException("Capacity values must be finite numbers.");

            if (dto.AvailableCapacity < 0 || dto.ReservedCapacity < 0 || dto.UsedCapacity < 0)
                throw new ArgumentException("Capacity allocations cannot be negative.");

            if (dto.AvailableCapacity > dto.TotalCapacity)
                throw new ArgumentException("Available capacity cannot exceed total capacity.");

            if (dto.UsedCapacity + dto.ReservedCapacity > dto.TotalCapacity)
                throw new ArgumentException("Sum of used capacity and reserved capacity cannot exceed total capacity.");

            var committed = microgrid.ReservedCapacity + microgrid.UsedCapacity;
            if (dto.TotalCapacity < committed)
                throw new ArgumentException("Total capacity cannot be reduced below currently reserved and used capacity.");

            if (Math.Abs(dto.AvailableCapacity + dto.ReservedCapacity + dto.UsedCapacity - dto.TotalCapacity) > 0.000001)
                throw new ArgumentException("Available, reserved, and used capacity must add up to total capacity.");

            if (Math.Abs(dto.ReservedCapacity - microgrid.ReservedCapacity) > 0.000001 || Math.Abs(dto.UsedCapacity - microgrid.UsedCapacity) > 0.000001)
                throw new ArgumentException("Reserved and used capacity are maintained by slot and usage operations; only total capacity may be adjusted.");

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
