// ===========================================================================================================
// File: EnergySlotValidator.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: FluentValidation rules for EnergySlot data integrity.
// ===========================================================================================================
using System;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.M1;

namespace SmartMicrogrid.API.Validators
{
    public static class EnergySlotValidator
    {
        /// <summary>
        /// Verifies and validates te create slot criteria.
        /// </summary>
        public static (bool isValid, string? errorMessage) ValidateCreateSlot(CreateEnergySlotDto dto, MicrogridNode? microgrid)
        {
            // Execute validate create slot operations
            if (microgrid == null)
                return (false, "Target microgrid does not exist.");

            if (!microgrid.IsActive || microgrid.Status != "Active")
                return (false, $"Cannot publish energy slot for microgrid in '{microgrid.Status}' status or inactive state.");

            if (dto.EnergyAmount <= 0)
                return (false, "Energy amount must be greater than zero.");

            var available = dto.AvailableAmount ?? dto.EnergyAmount;
            if (available < 0)
                return (false, "Available amount cannot be negative.");

            if (available > dto.EnergyAmount)
                return (false, "Available amount cannot exceed total energy amount.");

            if (dto.StartTime >= dto.EndTime)
                return (false, "Slot start time must be strictly earlier than end time.");

            if (dto.PricePerUnit < 0)
                return (false, "Price per unit cannot be negative.");

            if (dto.EnergyAmount > microgrid.Capacity)
                return (false, $"Slot energy amount ({dto.EnergyAmount} kWh) cannot exceed total microgrid capacity ({microgrid.Capacity} kWh).");

            return (true, null);
        }
        /// <summary>
        /// Verifies and validates te update slot criteria.
        /// </summary>

        public static (bool isValid, string? errorMessage) ValidateUpdateSlot(UpdateEnergySlotDto dto, MicrogridNode? microgrid)
        {
            // Execute validate update slot operations
            if (dto.EnergyAmount <= 0)
                return (false, "Energy amount must be greater than zero.");

            if (dto.AvailableAmount < 0)
                return (false, "Available amount cannot be negative.");

            if (dto.AvailableAmount > dto.EnergyAmount)
                return (false, "Available amount cannot exceed total energy amount.");

            if (dto.StartTime >= dto.EndTime)
                return (false, "Slot start time must be strictly earlier than end time.");

            if (dto.PricePerUnit < 0)
                return (false, "Price per unit cannot be negative.");

            if (microgrid != null && dto.EnergyAmount > microgrid.Capacity)
                return (false, $"Slot energy amount ({dto.EnergyAmount} kWh) cannot exceed microgrid capacity ({microgrid.Capacity} kWh).");

            return (true, null);
        }
    }
}
