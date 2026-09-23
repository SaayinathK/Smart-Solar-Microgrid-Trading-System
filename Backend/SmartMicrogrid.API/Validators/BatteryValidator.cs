using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Validators
{
    public static class BatteryValidator
    {
        public static (bool isValid, string? errorMessage) ValidateBattery(UpdateBatteryDto dto)
        {
            if (dto.BatteryCapacity < 0)
                return (false, "Battery capacity cannot be negative.");

            if (dto.CurrentBatteryLevel < 0)
                return (false, "Current battery level cannot be negative.");

            if (dto.BatteryCapacity > 0 && dto.CurrentBatteryLevel > dto.BatteryCapacity)
                return (false, "Current battery level cannot exceed total battery capacity.");

            return (true, null);
        }

        public static double CalculatePercentage(double currentLevel, double capacity)
        {
            if (capacity <= 0) return 0.0;
            var pct = (currentLevel / capacity) * 100.0;
            return pct > 100.0 ? 100.0 : (pct < 0.0 ? 0.0 : pct);
        }

        public static string DetermineBatteryStatus(double percentage, double capacity)
        {
            if (capacity <= 0) return "Unavailable";
            if (percentage >= 95.0) return "Full";
            if (percentage <= 20.0) return "Low";
            return "Normal";
        }
    }
}
