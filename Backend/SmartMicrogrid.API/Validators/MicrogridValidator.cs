// ===========================================================================================================
// File: MicrogridValidator.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: FluentValidation rules for Microgrid data integrity.
// ===========================================================================================================
using System;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Validators
{
    public static class MicrogridValidator
    {
        /// <summary>
        /// Verifies and validates te create criteria.
        /// </summary>
        public static (bool isValid, string? errorMessage) ValidateCreate(CreateMicrogridDto dto)
        {
            // Execute validate create operations
            if (string.IsNullOrWhiteSpace(dto.Name))
                return (false, "Microgrid name cannot be empty.");

            if (string.IsNullOrWhiteSpace(dto.Location))
                return (false, "Location cannot be empty.");

            if (dto.Capacity <= 0)
                return (false, "Total generation capacity must be greater than zero.");

            if (dto.BatteryCapacity < 0)
                return (false, "Battery capacity cannot be negative.");

            if (dto.BatteryStorageSlots < 0)
                return (false, "Battery storage slots cannot be negative.");

            if (dto.CurrentBatteryLevel < 0 || (dto.BatteryCapacity > 0 && dto.CurrentBatteryLevel > dto.BatteryCapacity))
                return (false, "Current battery level must be between 0 and total battery capacity.");

            if (dto.Latitude < -90.0 || dto.Latitude > 90.0)
                return (false, "Latitude must be between -90 and 90 degrees.");

            if (dto.Longitude < -180.0 || dto.Longitude > 180.0)
                return (false, "Longitude must be between -180 and 180 degrees.");

            return (true, null);
        }
        /// <summary>
        /// Verifies and validates te update criteria.
        /// </summary>

        public static (bool isValid, string? errorMessage) ValidateUpdate(UpdateMicrogridDto dto)
        {
            // Execute validate update operations
            if (string.IsNullOrWhiteSpace(dto.Name))
                return (false, "Microgrid name cannot be empty.");

            if (string.IsNullOrWhiteSpace(dto.Location))
                return (false, "Location cannot be empty.");

            if (dto.Capacity <= 0)
                return (false, "Total generation capacity must be greater than zero.");

            if (dto.BatteryCapacity < 0)
                return (false, "Battery capacity cannot be negative.");

            if (dto.BatteryStorageSlots < 0)
                return (false, "Battery storage slots cannot be negative.");

            if (dto.Latitude < -90.0 || dto.Latitude > 90.0)
                return (false, "Latitude must be between -90 and 90 degrees.");

            if (dto.Longitude < -180.0 || dto.Longitude > 180.0)
                return (false, "Longitude must be between -180 and 180 degrees.");

            return (true, null);
        }
    }
}
