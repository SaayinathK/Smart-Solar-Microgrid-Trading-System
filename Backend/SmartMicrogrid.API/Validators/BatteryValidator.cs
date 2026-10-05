// ===========================================================================================================
// File: BatteryValidator.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: FluentValidation rules for Battery data integrity.
// ===========================================================================================================
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Validators
{
    public static class BatteryValidator
    {
        /// <summary>
        /// Verifies and validates te battery criteria.
        /// </summary>
        public static (bool isValid, string? errorMessage) ValidateBattery(UpdateBatteryDto dto)
        {
            // Execute validate battery operations
            if (dto.BatteryCapacity < 0)
                return (false, "Battery capacity cannot be negative.");

            if (dto.CurrentBatteryLevel < 0)
                return (false, "Current battery level cannot be negative.");

            if (dto.BatteryCapacity > 0 && dto.CurrentBatteryLevel > dto.BatteryCapacity)
                return (false, "Current battery level cannot exceed total battery capacity.");

            return (true, null);
        }
        /// <summary>
        /// Calculates and computes percentage.
        /// </summary>

        public static double CalculatePercentage(double currentLevel, double capacity)
        {
            // Execute calculate percentage operations
            if (capacity <= 0) return 0.0;
            var pct = (currentLevel / capacity) * 100.0;
            return pct > 100.0 ? 100.0 : (pct < 0.0 ? 0.0 : pct);
        }
        /// <summary>
        /// Performs determine battery status operation.
        /// </summary>

        public static string DetermineBatteryStatus(double percentage, double capacity)
        {
            // Execute determine battery status operations
            if (capacity <= 0) return "Unavailable";
            if (percentage >= 95.0) return "Full";
            if (percentage <= 20.0) return "Low";
            return "Normal";
        }
    }
}
