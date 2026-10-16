// ===========================================================================================================
// File: MicrogridValidationTests.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Unit/Integration test suite verifying MicrogridValidation operations and validations.
// ===========================================================================================================
using Xunit;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Validators;

namespace SmartMicrogrid.API.Tests
{
    public class MicrogridValidationTests
    {
        /// <summary>
        /// Creates or registers a new microgrid_valid data_returns true record.
        /// </summary>
        [Fact]
        public void CreateMicrogrid_ValidData_ReturnsTrue()
        {
            // Execute create microgrid_valid data_returns true operations
            var dto = new CreateMicrogridDto
            {
                Name = "Solar Hub A",
                Location = "Colombo",
                Capacity = 500,
                BatteryCapacity = 100,
                CurrentBatteryLevel = 50,
                Latitude = 6.9,
                Longitude = 79.8
            };

            var (isValid, errorMessage) = MicrogridValidator.ValidateCreate(dto);
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }
        /// <summary>
        /// Creates or registers a new microgrid_zero capacity_returns false record.
        /// </summary>

        [Fact]
        public void CreateMicrogrid_ZeroCapacity_ReturnsFalse()
        {
            // Execute create microgrid_zero capacity_returns false operations
            var dto = new CreateMicrogridDto
            {
                Name = "Solar Hub B",
                Location = "Kandy",
                Capacity = 0,
                Latitude = 7.2,
                Longitude = 80.6
            };

            var (isValid, errorMessage) = MicrogridValidator.ValidateCreate(dto);
            Assert.False(isValid);
            Assert.Contains("capacity", errorMessage?.ToLower());
        }
        /// <summary>
        /// Creates or registers a new microgrid_invalid gps_returns false record.
        /// </summary>

        [Fact]
        public void CreateMicrogrid_InvalidGPS_ReturnsFalse()
        {
            // Execute create microgrid_invalid gps_returns false operations
            var dto = new CreateMicrogridDto
            {
                Name = "Solar Hub C",
                Location = "Galle",
                Capacity = 100,
                Latitude = 150.0, // Invalid latitude
                Longitude = 80.0
            };

            var (isValid, errorMessage) = MicrogridValidator.ValidateCreate(dto);
            Assert.False(isValid);
            Assert.Contains("latitude", errorMessage?.ToLower());
        }
    }
}
