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

        public MicrogridService(IMicrogridRepository repository, IReservationRepository reservations)
        {
            _repository = repository;
            _reservations = reservations;
        }

        public async Task<IEnumerable<MicrogridResponseDto>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null)
        {
            var nodes = await _repository.GetAllAsync(status, isActive, location, search);
            return nodes.Select(MapToResponseDto);
        }

        public async Task<MicrogridResponseDto?> GetByIdAsync(string id)
        {
            var node = await _repository.GetByIdAsync(id);
            return node == null ? null : MapToResponseDto(node);
        }

        public async Task<MicrogridResponseDto> CreateAsync(CreateMicrogridDto dto, string operatorId)
        {
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

        public async Task<MicrogridResponseDto?> UpdateAsync(string id, UpdateMicrogridDto dto)
        {
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

        public async Task<bool> DeleteAsync(string id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
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

        private async Task EnsureCanDeactivateAsync(string id, string status, bool isActive)
        {
            if (!isActive && string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase) && await _reservations.HasActiveForNodeAsync(id))
            {
                throw new InvalidOperationException("This microgrid cannot be deactivated while active energy reservations exist.");
            }
        }

        private static MicrogridResponseDto MapToResponseDto(MicrogridNode node)
        {
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
