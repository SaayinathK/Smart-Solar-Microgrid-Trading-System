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
using SmartMicrogrid.API.Models.Common;
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
        private readonly IUserRepository? _users;
        /// <summary>
        /// Initializes a new instance of the MicrogridService class.
        /// </summary>

        public MicrogridService(
            IMicrogridRepository repository,
            IReservationRepository reservations,
            IUserRepository? users = null)
        {
            // Initialize dependencies and state
            _repository = repository;
            _reservations = reservations;
            _users = users;
        }
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        public async Task<IEnumerable<MicrogridResponseDto>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null, string? operatorId = null)
        {
            // Execute get all async operations
            var nodes = await _repository.GetAllAsync(status, isActive, location, search);
            if (!string.IsNullOrWhiteSpace(operatorId))
            {
                nodes = nodes.Where(n => string.Equals(n.OperatorId, operatorId, StringComparison.OrdinalIgnoreCase));
            }
            var dtos = nodes.Select(MapToResponseDto).ToList();
            await PopulateOperatorDetailsAsync(dtos);
            return dtos;
        }
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<MicrogridResponseDto?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            var node = await _repository.GetByIdAsync(id);
            if (node == null) return null;
            var dto = MapToResponseDto(node);
            await PopulateOperatorDetailsAsync(dto);
            return dto;
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
            var response = MapToResponseDto(created);
            await PopulateOperatorDetailsAsync(response);
            return response;
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
            if (!success) return null;
            var response = MapToResponseDto(existing);
            await PopulateOperatorDetailsAsync(response);
            return response;
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

        private async Task PopulateOperatorDetailsAsync(IEnumerable<MicrogridResponseDto> dtos)
        {
            if (_users == null) return;
            var list = dtos.ToList();
            if (list.Count == 0) return;

            var operatorIds = list
                .Select(d => d.OperatorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (operatorIds.Count == 0) return;

            var userMap = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
            foreach (var opId in operatorIds)
            {
                try
                {
                    var u = await _users.GetByIdAsync(opId);
                    if (u != null)
                    {
                        userMap[opId] = u;
                    }
                }
                catch
                {
                    // Ignore individual user lookup errors
                }
            }

            foreach (var dto in list)
            {
                if (!string.IsNullOrWhiteSpace(dto.OperatorId) && userMap.TryGetValue(dto.OperatorId, out var u))
                {
                    dto.OperatorName = $"{u.FirstName} {u.LastName}".Trim();
                    dto.OperatorEmail = u.Email;
                }
            }
        }

        private async Task PopulateOperatorDetailsAsync(MicrogridResponseDto dto)
        {
            if (_users == null || string.IsNullOrWhiteSpace(dto.OperatorId)) return;
            try
            {
                var u = await _users.GetByIdAsync(dto.OperatorId);
                if (u != null)
                {
                    dto.OperatorName = $"{u.FirstName} {u.LastName}".Trim();
                    dto.OperatorEmail = u.Email;
                }
            }
            catch
            {
                // Ignore individual lookup errors
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
