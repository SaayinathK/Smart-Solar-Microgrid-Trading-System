// ===========================================================================================================
// File: ConfigurationDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Data transfer object (DTO) representing ConfigurationDto communication payload.
// ===========================================================================================================
using System;
using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.M4
{
    public class UpdateConfigurationDto
    {
        [Required(ErrorMessage = "Platform name is required.")]
        [StringLength(120, MinimumLength = 3, ErrorMessage = "Platform name must be between 3 and 120 characters.")]
        public string PlatformName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Platform description cannot exceed 500 characters.")]
        public string PlatformDescription { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Maintenance message cannot exceed 300 characters.")]
        public string? MaintenanceMessage { get; set; }

        [Range(5, 1440, ErrorMessage = "Session timeout must be between 5 and 1440 minutes.")]
        public int SessionTimeoutMinutes { get; set; } = 480;

        [Range(1, 20, ErrorMessage = "Maximum login attempts must be between 1 and 20.")]
        public int MaxLoginAttempts { get; set; } = 5;

        [Range(5, 200, ErrorMessage = "Default page size must be between 5 and 200.")]
        public int DefaultPageSize { get; set; } = 20;
    }

    public class MaintenanceModeDto
    {
        [Required(ErrorMessage = "maintenanceMode is required.")]
        public bool MaintenanceMode { get; set; }

        [StringLength(300, ErrorMessage = "Maintenance message cannot exceed 300 characters.")]
        public string? MaintenanceMessage { get; set; }
    }

    public class RegistrationModeDto
    {
        [Required(ErrorMessage = "allowRegistration is required.")]
        public bool AllowRegistration { get; set; }
    }

    public class ConfigurationResponseDto
    {
        public string PlatformName { get; set; } = string.Empty;
        public string PlatformDescription { get; set; } = string.Empty;
        public bool MaintenanceMode { get; set; }
        public string? MaintenanceMessage { get; set; }
        public bool AllowRegistration { get; set; }
        public int SessionTimeoutMinutes { get; set; }
        public int MaxLoginAttempts { get; set; }
        public int DefaultPageSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class SystemHealthDto
    {
        public string Api { get; set; } = "Healthy";
        public string Database { get; set; } = "Unknown";
        public string DatabaseDetail { get; set; } = string.Empty;
        public string Authentication { get; set; } = "Healthy";
        public string Server { get; set; } = "Online";
        public string Environment { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;
        public double DatabaseLatencyMs { get; set; }
        public long ProcessMemoryBytes { get; set; }
        public TimeSpan Uptime { get; set; }
        public string JwtIssuer { get; set; } = string.Empty;
    }
}
