using Xunit;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Validators;

namespace SmartMicrogrid.API.Tests
{
    public class MicrogridValidationTests
    {
        [Fact]
        public void CreateMicrogrid_ValidData_ReturnsTrue()
        {
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

        [Fact]
        public void CreateMicrogrid_ZeroCapacity_ReturnsFalse()
        {
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

        [Fact]
        public void CreateMicrogrid_InvalidGPS_ReturnsFalse()
        {
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
