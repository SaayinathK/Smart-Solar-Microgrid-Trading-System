// ===========================================================================================================
// File: BatteryValidationTests.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Unit/Integration test suite verifying BatteryValidation operations and validations.
// ===========================================================================================================
using Xunit;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Validators;

namespace SmartMicrogrid.API.Tests
{
    public class BatteryValidationTests
    {
        /// <summary>
        /// Calculates and computes percentage_valid inputs_returns correct percentage.
        /// </summary>
        [Fact]
        public void CalculatePercentage_ValidInputs_ReturnsCorrectPercentage()
        {
            // Execute calculate percentage_valid inputs_returns correct percentage operations
            var pct = BatteryValidator.CalculatePercentage(75, 100);
            Assert.Equal(75.0, pct);
        }
        /// <summary>
        /// Calculates and computes percentage_zero capacity_returns zero.
        /// </summary>

        [Fact]
        public void CalculatePercentage_ZeroCapacity_ReturnsZero()
        {
            // Execute calculate percentage_zero capacity_returns zero operations
            var pct = BatteryValidator.CalculatePercentage(50, 0);
            Assert.Equal(0.0, pct);
        }
        /// <summary>
        /// Performs determine status_full battery_returns full operation.
        /// </summary>

        [Fact]
        public void DetermineStatus_FullBattery_ReturnsFull()
        {
            // Execute determine status_full battery_returns full operations
            var status = BatteryValidator.DetermineBatteryStatus(98.0, 100);
            Assert.Equal("Full", status);
        }
        /// <summary>
        /// Performs determine status_low battery_returns low operation.
        /// </summary>

        [Fact]
        public void DetermineStatus_LowBattery_ReturnsLow()
        {
            // Execute determine status_low battery_returns low operations
            var status = BatteryValidator.DetermineBatteryStatus(15.0, 100);
            Assert.Equal("Low", status);
        }
        /// <summary>
        /// Verifies and validates te battery_negative level_returns false criteria.
        /// </summary>

        [Fact]
        public void ValidateBattery_NegativeLevel_ReturnsFalse()
        {
            // Execute validate battery_negative level_returns false operations
            var dto = new UpdateBatteryDto { BatteryCapacity = 100, CurrentBatteryLevel = -10 };
            var (isValid, errorMessage) = BatteryValidator.ValidateBattery(dto);
            Assert.False(isValid);
            Assert.Contains("negative", errorMessage?.ToLower());
        }
        /// <summary>
        /// Verifies and validates te battery_level exceeds capacity_returns false criteria.
        /// </summary>

        [Fact]
        public void ValidateBattery_LevelExceedsCapacity_ReturnsFalse()
        {
            // Execute validate battery_level exceeds capacity_returns false operations
            var dto = new UpdateBatteryDto { BatteryCapacity = 100, CurrentBatteryLevel = 150 };
            var (isValid, errorMessage) = BatteryValidator.ValidateBattery(dto);
            Assert.False(isValid);
            Assert.Contains("exceed", errorMessage?.ToLower());
        }
    }
}
