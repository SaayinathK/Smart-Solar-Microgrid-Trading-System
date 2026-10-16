// ===========================================================================================================
// File: MicrogridService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Business logic service implementing MicrogridService operations, rules, and workflows.
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
    public class MicrogridService : IMicrogridService
    {
        private readonly IMicrogridRepository _repository;
        private readonly IReservationRepository _reservations;
        /// <summary>
        /// Initializes a new instance of the MicrogridService class.
        /// </summary>

        public MicrogridService(IMicrogridRepository repository, IReservationRepository reservations)
        {
            // Initialize dependencies and state
            _repository = repository;
            _reservations = reservations;
        }
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        public async Task<IEnumerable<MicrogridResponseDto>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null)
        {
            // Execute get all async operations
            var nodes = await _repository.GetAllAsync(status, isActive, location, search);
            return nodes.Select(MapToResponseDto);
        }
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<MicrogridResponseDto?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            var node = await _repository.GetByIdAsync(id);
            return node == null ? null : MapToResponseDto(node);
        }
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>

        public async Task<MicrogridResponseDto> CreateAsync(CreateMicrogridDto dto, string operatorId)
        {
            // Execute create async operations
            var (isValid, errorMessage) = MicrogridValidator.ValidateCreate(dto);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            var batteryPct = BatteryValidator.CalculatePercentage(dto.CurrentBatteryLevel, dto.BatteryCapacity);

            var node = new MicrogridNode
            {
                Name = dto.Name.Trim(),
                Location = dto.Location.Trim(),
                Description = dto.Description?.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Capacity = dto.Capacity,
                AvailableCapacity = dto.Capacity, // Initially 100% available
                ReservedCapacity = 0,
                UsedCapacity = 0,
                BatteryCapacity = dto.BatteryCapacity,
                BatteryStorageSlots = dto.BatteryStorageSlots,
                CurrentBatteryLevel = dto.CurrentBatteryLevel,
                BatteryPercentage = batteryPct,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
                IsActive = dto.IsActive,
                OperatorId = string.IsNullOrWhiteSpace(dto.OperatorId) ? operatorId : dto.OperatorId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(node);
            return MapToResponseDto(created);
        }
        /// <summary>
        /// Updates the specified async record.
        /// </summary>

        public async Task<MicrogridResponseDto?> UpdateAsync(string id, UpdateMicrogridDto dto)
        {
            // Execute update async operations
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            var (isValid, errorMessage) = MicrogridValidator.ValidateUpdate(dto);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            await EnsureCanDeactivateAsync(existing.Id!, dto.Status, dto.IsActive);

            existing.Name = dto.Name.Trim();
            existing.Location = dto.Location.Trim();
            existing.Description = dto.Description?.Trim();
            existing.Latitude = dto.Latitude;
            existing.Longitude = dto.Longitude;
            existing.Capacity = dto.Capacity;
            existing.BatteryCapacity = dto.BatteryCapacity;
            existing.BatteryStorageSlots = dto.BatteryStorageSlots;
            existing.Status = dto.Status;
            existing.IsActive = dto.IsActive;
            if (!string.IsNullOrWhiteSpace(dto.OperatorId))
            {
                existing.OperatorId = dto.OperatorId;
            }

            // Re-calculate available capacity if capacity updated
            if (existing.AvailableCapacity > existing.Capacity)
            {
                existing.AvailableCapacity = existing.Capacity - existing.ReservedCapacity - existing.UsedCapacity;
                if (existing.AvailableCapacity < 0) existing.AvailableCapacity = 0;
            }

            existing.BatteryPercentage = BatteryValidator.CalculatePercentage(existing.CurrentBatteryLevel, existing.BatteryCapacity);

            var success = await _repository.UpdateAsync(id, existing);
            return success ? MapToResponseDto(existing) : null;
        }
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>

        public async Task<bool> DeleteAsync(string id)
        {
            // Execute delete async operations
            return await _repository.DeleteAsync(id);
        }
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>

        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
            // Execute update status async operations
            var validStatuses = new[] { "Active", "Inactive", "Maintenance", "Offline" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid status '{status}'. Allowed values: Active, Inactive, Maintenance, Offline.");
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;
            await EnsureCanDeactivateAsync(id, status, string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase));

            bool isActive = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
            return await _repository.UpdateStatusAsync(id, status, isActive);
        }
        /// <summary>
        /// Seeds and configures default/sample re can deactivate async data.
        /// </summary>

        private async Task EnsureCanDeactivateAsync(string id, string status, bool isActive)
        {
            // Execute ensure can deactivate async operations
            if (!isActive && string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase) && await _reservations.HasActiveForNodeAsync(id))
            {
                throw new InvalidOperationException("This microgrid cannot be deactivated while active energy reservations exist.");
            }
        }
        /// <summary>
        /// Performs map to response dto operation.
        /// </summary>

        private static MicrogridResponseDto MapToResponseDto(MicrogridNode node)
        {
            // Execute map to response dto operations
            return new MicrogridResponseDto
            {
                Id = node.Id ?? string.Empty,
                Name = node.Name,
                Location = node.Location,
                Description = node.Description,
                Latitude = node.Latitude,
                Longitude = node.Longitude,
                Capacity = node.Capacity,
                AvailableCapacity = node.AvailableCapacity,
                ReservedCapacity = node.ReservedCapacity,
                UsedCapacity = node.UsedCapacity,
                BatteryCapacity = node.BatteryCapacity,
                BatteryStorageSlots = node.BatteryStorageSlots,
                CurrentBatteryLevel = node.CurrentBatteryLevel,
                BatteryPercentage = node.BatteryPercentage,
                Status = node.Status,
                IsActive = node.IsActive,
                OperatorId = node.OperatorId,
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt
            };
        }
    }
}
