// ===========================================================================================================
// File: EnergySlotValidationTests.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Unit/Integration test suite verifying EnergySlotValidation operations and validations.
// ===========================================================================================================
using System;
using Xunit;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Validators;

namespace SmartMicrogrid.API.Tests
{
    public class EnergySlotValidationTests
    {
        private readonly MicrogridNode _activeMicrogrid = new MicrogridNode
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Active Hub",
            Capacity = 500,
            Status = "Active",
            IsActive = true
        };

        private readonly MicrogridNode _offlineMicrogrid = new MicrogridNode
        {
            Id = "507f1f77bcf86cd799439022",
            Name = "Offline Hub",
            Capacity = 500,
            Status = "Offline",
            IsActive = false
        };
        /// <summary>
        /// Verifies and validates te create slot_valid data_returns true criteria.
        /// </summary>

        [Fact]
        public void ValidateCreateSlot_ValidData_ReturnsTrue()
        {
            // Execute validate create slot_valid data_returns true operations
            var dto = new CreateEnergySlotDto
            {
                MicrogridNodeId = _activeMicrogrid.Id!,
                EnergyAmount = 50,
                AvailableAmount = 50,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(3),
                PricePerUnit = 20.0m
            };

            var (isValid, errorMessage) = EnergySlotValidator.ValidateCreateSlot(dto, _activeMicrogrid);
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }
        /// <summary>
        /// Verifies and validates te create slot_offline microgrid_returns false criteria.
        /// </summary>

        [Fact]
        public void ValidateCreateSlot_OfflineMicrogrid_ReturnsFalse()
        {
            // Execute validate create slot_offline microgrid_returns false operations
            var dto = new CreateEnergySlotDto
            {
                MicrogridNodeId = _offlineMicrogrid.Id!,
                EnergyAmount = 50,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(3),
                PricePerUnit = 20.0m
            };

            var (isValid, errorMessage) = EnergySlotValidator.ValidateCreateSlot(dto, _offlineMicrogrid);
            Assert.False(isValid);
            Assert.Contains("offline", errorMessage?.ToLower());
        }
        /// <summary>
        /// Verifies and validates te create slot_start time after end time_returns false criteria.
        /// </summary>

        [Fact]
        public void ValidateCreateSlot_StartTimeAfterEndTime_ReturnsFalse()
        {
            // Execute validate create slot_start time after end time_returns false operations
            var dto = new CreateEnergySlotDto
            {
                MicrogridNodeId = _activeMicrogrid.Id!,
                EnergyAmount = 50,
                StartTime = DateTime.UtcNow.AddHours(5),
                EndTime = DateTime.UtcNow.AddHours(1), // invalid
                PricePerUnit = 20.0m
            };

            var (isValid, errorMessage) = EnergySlotValidator.ValidateCreateSlot(dto, _activeMicrogrid);
            Assert.False(isValid);
            Assert.Contains("earlier", errorMessage?.ToLower());
        }
        /// <summary>
        /// Verifies and validates te create slot_energy exceeds capacity_returns false criteria.
        /// </summary>

        [Fact]
        public void ValidateCreateSlot_EnergyExceedsCapacity_ReturnsFalse()
        {
            // Execute validate create slot_energy exceeds capacity_returns false operations
            var dto = new CreateEnergySlotDto
            {
                MicrogridNodeId = _activeMicrogrid.Id!,
                EnergyAmount = 1000, // Exceeds 500
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(3),
                PricePerUnit = 20.0m
            };

            var (isValid, errorMessage) = EnergySlotValidator.ValidateCreateSlot(dto, _activeMicrogrid);
            Assert.False(isValid);
            Assert.Contains("exceed", errorMessage?.ToLower());
        }
    }
}
