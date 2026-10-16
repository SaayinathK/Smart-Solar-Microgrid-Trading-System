// ===========================================================================================================
// File: BatteryService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Business logic service implementing BatteryService operations, rules, and workflows.
// ===========================================================================================================
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
        /// <summary>
        /// Initializes a new instance of the BatteryService class.
        /// </summary>

        public BatteryService(IMicrogridRepository repository)
        {
            // Initialize dependencies and state
            _repository = repository;
        }
        /// <summary>
        /// Retrieves battery async details.
        /// </summary>

        public async Task<BatteryResponseDto?> GetBatteryAsync(string microgridId)
        {
            // Execute get battery async operations
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
        /// <summary>
        /// Updates the specified battery async record.
        /// </summary>

        public async Task<BatteryResponseDto?> UpdateBatteryAsync(string microgridId, UpdateBatteryDto dto)
        {
            // Execute update battery async operations
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
