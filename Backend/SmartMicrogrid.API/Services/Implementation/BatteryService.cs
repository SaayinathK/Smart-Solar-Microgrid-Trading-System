using System;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;
using SmartMicrogrid.API.Validators;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class BatteryService : IBatteryService
    {
        private readonly IMicrogridRepository _repository;

        public BatteryService(IMicrogridRepository repository)
        {
            _repository = repository;
        }

        public async Task<BatteryResponseDto?> GetBatteryAsync(string microgridId)
        {
            var microgrid = await _repository.GetByIdAsync(microgridId);
            if (microgrid == null) return null;

            var pct = BatteryValidator.CalculatePercentage(microgrid.CurrentBatteryLevel, microgrid.BatteryCapacity);
            var status = BatteryValidator.DetermineBatteryStatus(pct, microgrid.BatteryCapacity);

            return new BatteryResponseDto
            {
                MicrogridId = microgrid.Id ?? string.Empty,
                BatteryCapacity = microgrid.BatteryCapacity,
                CurrentBatteryLevel = microgrid.CurrentBatteryLevel,
                BatteryPercentage = pct,
                Status = status
            };
        }

        public async Task<BatteryResponseDto?> UpdateBatteryAsync(string microgridId, UpdateBatteryDto dto)
        {
            var microgrid = await _repository.GetByIdAsync(microgridId);
            if (microgrid == null) return null;

            var (isValid, errorMessage) = BatteryValidator.ValidateBattery(dto);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            var pct = BatteryValidator.CalculatePercentage(dto.CurrentBatteryLevel, dto.BatteryCapacity);
            var status = BatteryValidator.DetermineBatteryStatus(pct, dto.BatteryCapacity);

            var success = await _repository.UpdateBatteryAsync(microgridId, dto.BatteryCapacity, dto.CurrentBatteryLevel, pct, status);
            if (!success) return null;

            return new BatteryResponseDto
            {
                MicrogridId = microgridId,
                BatteryCapacity = dto.BatteryCapacity,
                CurrentBatteryLevel = dto.CurrentBatteryLevel,
                BatteryPercentage = pct,
                Status = status
            };
        }
    }
}
