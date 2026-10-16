// ===========================================================================================================
// File: EnergySlotService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Business logic service implementing EnergySlotService operations, rules, and workflows.
// ===========================================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;
using SmartMicrogrid.API.Validators;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class EnergySlotService : IEnergySlotService
    {
        private readonly IEnergySlotRepository _slotRepository;
        private readonly IMicrogridRepository _microgridRepository;
        /// <summary>
        /// Initializes a new instance of the EnergySlotService class.
        /// </summary>

        public EnergySlotService(IEnergySlotRepository slotRepository, IMicrogridRepository microgridRepository)
        {
            // Initialize dependencies and state
            _slotRepository = slotRepository;
            _microgridRepository = microgridRepository;
        }
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        public async Task<IEnumerable<EnergySlotResponseDto>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null)
        {
            // Execute get all async operations
            var slots = await _slotRepository.GetAllAsync(microgridId, status, startTime, endTime, minEnergy);
            var microgridDict = await GetMicrogridDictionaryAsync(slots.Select(s => s.MicrogridNodeId));

            return slots.Select(s => MapToResponseDto(s, microgridDict.GetValueOrDefault(s.MicrogridNodeId)));
        }
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<EnergySlotResponseDto?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            var slot = await _slotRepository.GetByIdAsync(id);
            if (slot == null) return null;

            var microgrid = await _microgridRepository.GetByIdAsync(slot.MicrogridNodeId);
            return MapToResponseDto(slot, microgrid);
        }
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>

        public async Task<EnergySlotResponseDto> CreateAsync(CreateEnergySlotDto dto, string createdBy)
        {
            // Execute create async operations
            var microgrid = await _microgridRepository.GetByIdAsync(dto.MicrogridNodeId);
            var (isValid, errorMessage) = EnergySlotValidator.ValidateCreateSlot(dto, microgrid);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            var initialAvailable = dto.AvailableAmount ?? dto.EnergyAmount;

            var slot = new EnergySlot
            {
                MicrogridNodeId = dto.MicrogridNodeId,
                EnergyAmount = dto.EnergyAmount,
                AvailableAmount = initialAvailable,
                StartTime = dto.StartTime.ToUniversalTime(),
                EndTime = dto.EndTime.ToUniversalTime(),
                PricePerUnit = dto.PricePerUnit,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Available" : dto.Status,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _slotRepository.CreateAsync(slot);

            // Deduct available capacity on the microgrid
            if (microgrid != null)
            {
                var newAvailable = microgrid.AvailableCapacity - dto.EnergyAmount;
                if (newAvailable < 0) newAvailable = 0;
                var newReserved = microgrid.ReservedCapacity + dto.EnergyAmount;
                await _microgridRepository.UpdateCapacityAsync(microgrid.Id!, microgrid.Capacity, newAvailable, newReserved, microgrid.UsedCapacity);
            }

            return MapToResponseDto(created, microgrid);
        }
        /// <summary>
        /// Updates the specified async record.
        /// </summary>

        public async Task<EnergySlotResponseDto?> UpdateAsync(string id, UpdateEnergySlotDto dto)
        {
            // Execute update async operations
            var existing = await _slotRepository.GetByIdAsync(id);
            if (existing == null) return null;

            var microgrid = await _microgridRepository.GetByIdAsync(existing.MicrogridNodeId);
            var (isValid, errorMessage) = EnergySlotValidator.ValidateUpdateSlot(dto, microgrid);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            existing.EnergyAmount = dto.EnergyAmount;
            existing.AvailableAmount = dto.AvailableAmount;
            existing.StartTime = dto.StartTime.ToUniversalTime();
            existing.EndTime = dto.EndTime.ToUniversalTime();
            existing.PricePerUnit = dto.PricePerUnit;
            existing.Status = dto.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            var success = await _slotRepository.UpdateAsync(id, existing);
            return success ? MapToResponseDto(existing, microgrid) : null;
        }
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>

        public async Task<bool> DeleteAsync(string id)
        {
            // Execute delete async operations
            return await _slotRepository.DeleteAsync(id);
        }
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>

        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
            // Execute update status async operations
            var validStatuses = new[] { "Available", "PartiallyReserved", "FullyReserved", "Expired", "Cancelled" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid slot status '{status}'. Allowed values: Available, PartiallyReserved, FullyReserved, Expired, Cancelled.");
            }

            return await _slotRepository.UpdateStatusAsync(id, status);
        }
        /// <summary>
        /// Retrieves energy availability async details.
        /// </summary>

        public async Task<IEnumerable<EnergyAvailabilityResponseDto>> GetEnergyAvailabilityAsync(EnergyAvailabilityQueryDto query)
        {
            // Execute get energy availability async operations
            var slots = await _slotRepository.GetAllAsync(
                query.MicrogridId,
                query.Status ?? "Available",
                query.StartTime,
                query.EndTime,
                query.MinimumEnergy
            );

            // Filter out expired or cancelled slots
            var now = DateTime.UtcNow;
            slots = slots.Where(s => s.Status == "Available" && s.EndTime > now && s.AvailableAmount > 0);

            var microgridDict = await GetMicrogridDictionaryAsync(slots.Select(s => s.MicrogridNodeId));

            // Only show slots for Active microgrids
            slots = slots.Where(s => 
            {
                var mg = microgridDict.GetValueOrDefault(s.MicrogridNodeId);
                return mg != null && mg.Status == "Active";
            });

            if (!string.IsNullOrWhiteSpace(query.Location))
            {
                slots = slots.Where(s =>
                {
                    var mg = microgridDict.GetValueOrDefault(s.MicrogridNodeId);
                    return mg != null && mg.Location.Contains(query.Location, StringComparison.OrdinalIgnoreCase);
                });
            }

            return slots.Select(s =>
            {
                var mg = microgridDict.GetValueOrDefault(s.MicrogridNodeId);
                return new EnergyAvailabilityResponseDto
                {
                    EnergySlotId = s.Id ?? string.Empty,
                    MicrogridNodeId = s.MicrogridNodeId,
                    MicrogridName = mg?.Name ?? "Unknown Microgrid",
                    Location = mg?.Location ?? "Unknown Location",
                    EnergyAmount = s.EnergyAmount,
                    AvailableAmount = s.AvailableAmount,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    PricePerUnit = s.PricePerUnit,
                    Status = s.Status
                };
            });
        }
        /// <summary>
        /// Retrieves microgrid dictionary async details.
        /// </summary>

        private async Task<Dictionary<string, MicrogridNode>> GetMicrogridDictionaryAsync(IEnumerable<string> microgridIds)
        {
            // Execute get microgrid dictionary async operations
            var distinctIds = microgridIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var result = new Dictionary<string, MicrogridNode>();

            foreach (var id in distinctIds)
            {
                var mg = await _microgridRepository.GetByIdAsync(id);
                if (mg != null && mg.Id != null)
                {
                    result[mg.Id] = mg;
                }
            }

            return result;
        }
        /// <summary>
        /// Performs map to response dto operation.
        /// </summary>

        private static EnergySlotResponseDto MapToResponseDto(EnergySlot slot, MicrogridNode? microgrid)
        {
            // Execute map to response dto operations
            return new EnergySlotResponseDto
            {
                Id = slot.Id ?? string.Empty,
                MicrogridNodeId = slot.MicrogridNodeId,
                MicrogridName = microgrid?.Name ?? "Unknown Microgrid",
                Location = microgrid?.Location ?? "Unknown Location",
                EnergyAmount = slot.EnergyAmount,
                AvailableAmount = slot.AvailableAmount,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                PricePerUnit = slot.PricePerUnit,
                Status = slot.Status,
                CreatedBy = slot.CreatedBy,
                CreatedAt = slot.CreatedAt,
                UpdatedAt = slot.UpdatedAt
            };
        }
    }
}
