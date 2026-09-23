using System;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Validators
{
    public static class MicrogridValidator
    {
        public static (bool isValid, string? errorMessage) ValidateCreate(CreateMicrogridDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return (false, "Microgrid name cannot be empty.");

            if (string.IsNullOrWhiteSpace(dto.Location))
                return (false, "Location cannot be empty.");

            if (dto.Capacity <= 0)
                return (false, "Total generation capacity must be greater than zero.");

            if (dto.BatteryCapacity < 0)
                return (false, "Battery capacity cannot be negative.");

            if (dto.CurrentBatteryLevel < 0 || (dto.BatteryCapacity > 0 && dto.CurrentBatteryLevel > dto.BatteryCapacity))
                return (false, "Current battery level must be between 0 and total battery capacity.");

            if (dto.Latitude < -90.0 || dto.Latitude > 90.0)
                return (false, "Latitude must be between -90 and 90 degrees.");

            if (dto.Longitude < -180.0 || dto.Longitude > 180.0)
                return (false, "Longitude must be between -180 and 180 degrees.");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidateUpdate(UpdateMicrogridDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return (false, "Microgrid name cannot be empty.");

            if (string.IsNullOrWhiteSpace(dto.Location))
                return (false, "Location cannot be empty.");

            if (dto.Capacity <= 0)
                return (false, "Total generation capacity must be greater than zero.");

            if (dto.BatteryCapacity < 0)
                return (false, "Battery capacity cannot be negative.");

            if (dto.Latitude < -90.0 || dto.Latitude > 90.0)
                return (false, "Latitude must be between -90 and 90 degrees.");

            if (dto.Longitude < -180.0 || dto.Longitude > 180.0)
                return (false, "Longitude must be between -180 and 180 degrees.");

            return (true, null);
        }
    }
}
