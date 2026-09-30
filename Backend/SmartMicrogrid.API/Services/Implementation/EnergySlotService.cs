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

        public EnergySlotService(IEnergySlotRepository slotRepository, IMicrogridRepository microgridRepository)
        {
            _slotRepository = slotRepository;
            _microgridRepository = microgridRepository;
        }

        public async Task<IEnumerable<EnergySlotResponseDto>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null)
        {
            var slots = await _slotRepository.GetAllAsync(microgridId, status, startTime, endTime, minEnergy);
            var microgridDict = await GetMicrogridDictionaryAsync(slots.Select(s => s.MicrogridNodeId));

            return slots.Select(s => MapToResponseDto(s, microgridDict.GetValueOrDefault(s.MicrogridNodeId)));
        }

        public async Task<EnergySlotResponseDto?> GetByIdAsync(string id)
        {
            var slot = await _slotRepository.GetByIdAsync(id);
            if (slot == null) return null;

            var microgrid = await _microgridRepository.GetByIdAsync(slot.MicrogridNodeId);
            return MapToResponseDto(slot, microgrid);
        }

        public async Task<EnergySlotResponseDto> CreateAsync(CreateEnergySlotDto dto, string createdBy)
        {
            var microgrid = await _microgridRepository.GetByIdAsync(dto.MicrogridNodeId);
            var (isValid, errorMessage) = EnergySlotValidator.ValidateCreateSlot(dto, microgrid);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            // M1 publishes the whole slot amount; clients cannot publish a pre-booked slot.
            var initialAvailable = dto.EnergyAmount;

            if (microgrid == null || string.IsNullOrWhiteSpace(microgrid.Id) ||
                !await _microgridRepository.TryAdjustSlotCapacityAsync(microgrid.Id, dto.EnergyAmount))
                throw new InvalidOperationException("Insufficient available microgrid capacity to publish this slot.");

            var slot = new EnergySlot
            {
                MicrogridNodeId = dto.MicrogridNodeId,
                EnergyAmount = dto.EnergyAmount,
                AvailableAmount = initialAvailable,
                StartTime = dto.StartTime.ToUniversalTime(),
                EndTime = dto.EndTime.ToUniversalTime(),
                PricePerUnit = dto.PricePerUnit,
                Status = "Available",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            EnergySlot created;
            try
            {
                created = await _slotRepository.CreateAsync(slot);
            }
            catch
            {
                await _microgridRepository.TryAdjustSlotCapacityAsync(microgrid.Id!, -dto.EnergyAmount);
                throw;
            }

            return MapToResponseDto(created, microgrid);
        }

        public async Task<EnergySlotResponseDto?> UpdateAsync(string id, UpdateEnergySlotDto dto)
        {
            var existing = await _slotRepository.GetByIdAsync(id);
            if (existing == null) return null;

            var microgrid = await _microgridRepository.GetByIdAsync(existing.MicrogridNodeId);
            var (isValid, errorMessage) = EnergySlotValidator.ValidateUpdateSlot(dto, microgrid);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            var bookedAmount = existing.EnergyAmount - existing.AvailableAmount;
            if (dto.EnergyAmount < bookedAmount)
                throw new ArgumentException($"Slot total cannot be reduced below {bookedAmount} kWh already booked.");
            if (Math.Abs(dto.EnergyAmount - existing.EnergyAmount) > 0.000001 && bookedAmount > 0)
                throw new InvalidOperationException("Slot energy cannot be resized after M2 has booked part of it.");

            var capacityDelta = dto.EnergyAmount - existing.EnergyAmount;
            if (Math.Abs(capacityDelta) > 0.000001 &&
                (microgrid?.Id == null || !await _microgridRepository.TryAdjustSlotCapacityAsync(microgrid.Id, capacityDelta)))
                throw new InvalidOperationException("Insufficient available capacity to resize this slot.");

            existing.EnergyAmount = dto.EnergyAmount;
            existing.AvailableAmount = Math.Max(0, existing.AvailableAmount + capacityDelta);
            existing.StartTime = dto.StartTime.ToUniversalTime();
            existing.EndTime = dto.EndTime.ToUniversalTime();
            existing.PricePerUnit = dto.PricePerUnit;
            existing.Status = dto.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            existing.Status = DeriveStatus(existing);
            var success = await _slotRepository.UpdateAsync(id, existing);
            if (!success && Math.Abs(capacityDelta) > 0.000001 && microgrid?.Id != null)
                await _microgridRepository.TryAdjustSlotCapacityAsync(microgrid.Id, -capacityDelta);
            return success ? MapToResponseDto(existing, microgrid) : null;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var existing = await _slotRepository.GetByIdAsync(id);
            if (existing == null) return false;
            if (existing.EnergyAmount - existing.AvailableAmount > 0.000001)
                throw new InvalidOperationException("A slot with booked M2 energy cannot be deleted.");
            if (!await _microgridRepository.TryAdjustSlotCapacityAsync(existing.MicrogridNodeId, -existing.AvailableAmount))
                throw new InvalidOperationException("Unable to return slot energy to microgrid capacity.");
            if (await _slotRepository.DeleteAsync(id)) return true;
            await _microgridRepository.TryAdjustSlotCapacityAsync(existing.MicrogridNodeId, existing.AvailableAmount);
            return false;
        }

        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
            var validStatuses = new[] { "Available", "PartiallyReserved", "FullyReserved", "Expired", "Cancelled" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid slot status '{status}'. Allowed values: Available, PartiallyReserved, FullyReserved, Expired, Cancelled.");
            }

            var existing = await _slotRepository.GetByIdAsync(id);
            if (existing == null) return false;
            if (status.Equals("Expired", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Slot expiry is managed by the UTC cleanup service.");
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                if (existing.EnergyAmount - existing.AvailableAmount > 0.000001)
                    throw new InvalidOperationException("A slot with booked M2 energy cannot be cancelled.");
                var amountToRelease = existing.AvailableAmount;
                if (!await _microgridRepository.TryAdjustSlotCapacityAsync(existing.MicrogridNodeId, -amountToRelease))
                    throw new InvalidOperationException("Unable to return slot energy to microgrid capacity.");
                existing.AvailableAmount = 0;
                existing.Status = "Cancelled";
                var updated = await _slotRepository.UpdateAsync(id, existing);
                if (!updated)
                    await _microgridRepository.TryAdjustSlotCapacityAsync(existing.MicrogridNodeId, amountToRelease);
                return updated;
            }
            else if (status.Equals("PartiallyReserved", StringComparison.OrdinalIgnoreCase) || status.Equals("FullyReserved", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Reservation state is managed by M2.");
            else if (status.Equals("Available", StringComparison.OrdinalIgnoreCase) && existing.AvailableAmount < existing.EnergyAmount)
                throw new InvalidOperationException("A slot with M2 bookings cannot be marked fully available.");

            existing.Status = DeriveStatus(existing);
            return await _slotRepository.UpdateAsync(id, existing);
        }

        public async Task<IEnumerable<EnergyAvailabilityResponseDto>> GetEnergyAvailabilityAsync(EnergyAvailabilityQueryDto query)
        {
            var slots = await _slotRepository.GetAllAsync(
                query.MicrogridId,
                null,
                query.StartTime,
                query.EndTime,
                query.MinimumEnergy
            );

            // Filter out expired or cancelled slots
            var now = DateTime.UtcNow;
            slots = slots.Where(s => (s.Status == "Available" || s.Status == "PartiallyReserved") && s.StartTime > now && s.EndTime > now && s.AvailableAmount > 0);
            if (!string.IsNullOrWhiteSpace(query.Status))
                slots = slots.Where(s => string.Equals(s.Status, query.Status, StringComparison.OrdinalIgnoreCase));

            var microgridDict = await GetMicrogridDictionaryAsync(slots.Select(s => s.MicrogridNodeId));

            // Only show slots for Active microgrids
            slots = slots.Where(s => 
            {
                var mg = microgridDict.GetValueOrDefault(s.MicrogridNodeId);
                return mg != null && mg.IsActive && string.Equals(mg.Status, "Active", StringComparison.OrdinalIgnoreCase);
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

        private async Task<Dictionary<string, MicrogridNode>> GetMicrogridDictionaryAsync(IEnumerable<string> microgridIds)
        {
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

        private static string DeriveStatus(EnergySlot slot)
        {
            if (slot.EndTime <= DateTime.UtcNow) return "Expired";
            if (slot.AvailableAmount <= 0.000001) return "FullyReserved";
            if (slot.AvailableAmount + 0.000001 < slot.EnergyAmount) return "PartiallyReserved";
            return "Available";
        }

        private static EnergySlotResponseDto MapToResponseDto(EnergySlot slot, MicrogridNode? microgrid)
        {
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

