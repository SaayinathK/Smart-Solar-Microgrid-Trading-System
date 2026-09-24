using Xunit;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Validators;

namespace SmartMicrogrid.API.Tests
{
    public class BatteryValidationTests
    {
        [Fact]
        public void CalculatePercentage_ValidInputs_ReturnsCorrectPercentage()
        {
            var pct = BatteryValidator.CalculatePercentage(75, 100);
            Assert.Equal(75.0, pct);
        }

        [Fact]
        public void CalculatePercentage_ZeroCapacity_ReturnsZero()
        {
            var pct = BatteryValidator.CalculatePercentage(50, 0);
            Assert.Equal(0.0, pct);
        }

        [Fact]
        public void DetermineStatus_FullBattery_ReturnsFull()
        {
            var status = BatteryValidator.DetermineBatteryStatus(98.0, 100);
            Assert.Equal("Full", status);
        }

        [Fact]
        public void DetermineStatus_LowBattery_ReturnsLow()
        {
            var status = BatteryValidator.DetermineBatteryStatus(15.0, 100);
            Assert.Equal("Low", status);
        }

        [Fact]
        public void ValidateBattery_NegativeLevel_ReturnsFalse()
        {
            var dto = new UpdateBatteryDto { BatteryCapacity = 100, CurrentBatteryLevel = -10 };
            var (isValid, errorMessage) = BatteryValidator.ValidateBattery(dto);
            Assert.False(isValid);
            Assert.Contains("negative", errorMessage?.ToLower());
        }

        [Fact]
        public void ValidateBattery_LevelExceedsCapacity_ReturnsFalse()
        {
            var dto = new UpdateBatteryDto { BatteryCapacity = 100, CurrentBatteryLevel = 150 };
            var (isValid, errorMessage) = BatteryValidator.ValidateBattery(dto);
            Assert.False(isValid);
            Assert.Contains("exceed", errorMessage?.ToLower());
        }
    }
}
