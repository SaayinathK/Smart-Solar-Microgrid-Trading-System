using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Repositories.Interfaces.M4;
using SmartMicrogrid.API.Services.Implementation.M4;
using SmartMicrogrid.API.Services.Interfaces.M4;
using Xunit;

namespace SmartMicrogrid.API.Tests
{
    public class DashboardServiceTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ISystemActivityRepository> _activity = new();
        private readonly Mock<ISystemConfigurationRepository> _configuration = new();
        private readonly Mock<IMicrogridMonitorService> _microgrids = new();
        private readonly Mock<IReservationMonitorService> _reservations = new();
        private readonly Mock<ITransactionMonitorService> _transactions = new();
        private readonly Mock<ISystemHealthService> _health = new();

        public DashboardServiceTests()
        {
            _configuration.Setup(x => x.GetOrCreateAsync())
                .ReturnsAsync(new SystemConfiguration { PlatformName = "Smart Microgrid" });

            _activity.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<SystemActivity>());

            _health.Setup(x => x.GetHealthAsync())
                .ReturnsAsync(new SystemHealthDto { Api = "Healthy", Database = "Healthy" });

            SetupHealthyMonitors();
        }

        private void SetupHealthyMonitors()
        {
            _microgrids.Setup(x => x.GetCountAsync())
                .ReturnsAsync(new ComponentStatDto { Count = 4, Status = DataSourceStatus.Available });
            _microgrids.Setup(x => x.GetCapacitySummaryAsync())
                .ReturnsAsync(new ComponentStatDto
                {
                    Total = 500,
                    Available = 320,
                    Unit = "kWh",
                    Status = DataSourceStatus.Available
                });

            _reservations.Setup(x => x.GetCountAsync())
                .ReturnsAsync(new ComponentStatDto { Count = 12, Status = DataSourceStatus.Available });
            _reservations.Setup(x => x.GetPendingCountAsync())
                .ReturnsAsync(new ComponentStatDto { Count = 3, Status = DataSourceStatus.Available });

            _transactions.Setup(x => x.GetCountAsync())
                .ReturnsAsync(new ComponentStatDto { Count = 7, Status = DataSourceStatus.Available });
            _transactions.Setup(x => x.GetCompletedCountAsync())
                .ReturnsAsync(new ComponentStatDto { Count = 5, Status = DataSourceStatus.Available });
        }

        private DashboardService CreateService() => new(
            _users.Object,
            _activity.Object,
            _configuration.Object,
            _microgrids.Object,
            _reservations.Object,
            _transactions.Object,
            _health.Object);

        [Fact]
        public async Task BuildAsync_AggregatesM1M2M3Statistics()
        {
            _users.Setup(x => x.GetAllAsync(null, null, null, null))
                .ReturnsAsync(new List<User>());

            var dashboard = await CreateService().BuildAsync();

            Assert.Equal("Smart Microgrid", dashboard.PlatformName);
            Assert.Equal(4, dashboard.Platform.MicrogridCount);
            Assert.Equal(320, dashboard.Platform.TotalAvailableCapacity);
            Assert.Equal(12, dashboard.Platform.ReservationCount);
            Assert.Equal(3, dashboard.Platform.PendingReservationCount);
            Assert.Equal(7, dashboard.Platform.TransactionCount);
            Assert.Equal(5, dashboard.Platform.CompletedTransactionCount);
        }

        [Fact]
        public async Task BuildAsync_CountsEveryAccountStatusIncludingLegacyRecords()
        {
            _users.Setup(x => x.GetAllAsync(null, null, null, null))
                .ReturnsAsync(new List<User>
                {
                    // Explicit statuses.
                    new User { Id = "1", Role = Role.Admin, IsActive = true, AccountStatus = AccountStatus.Active },
                    new User { Id = "2", Role = Role.Prosumer, IsActive = false, AccountStatus = AccountStatus.Suspended },
                    new User { Id = "3", Role = Role.Prosumer, IsActive = false, AccountStatus = AccountStatus.Pending },
                    // Legacy document: no stored AccountStatus, so it deserializes to
                    // Active while IsActive says otherwise. It must land in Inactive.
                    new User { Id = "4", Role = Role.MicrogridOperator, IsActive = false }
                });

            var dashboard = await CreateService().BuildAsync();

            Assert.Equal(4, dashboard.Users.TotalUsers);
            Assert.Equal(1, dashboard.Users.ActiveUsers);
            Assert.Equal(1, dashboard.Users.InactiveUsers);
            Assert.Equal(1, dashboard.Users.SuspendedUsers);
            Assert.Equal(1, dashboard.Users.PendingUsers);
        }

        [Fact]
        public async Task BuildAsync_DerivesRoleDistributionForEveryRole()
        {
            _users.Setup(x => x.GetAllAsync(null, null, null, null))
                .ReturnsAsync(new List<User>
                {
                    new User { Id = "1", Role = Role.Admin, IsActive = true },
                    new User { Id = "2", Role = Role.Prosumer, IsActive = true },
                    new User { Id = "3", Role = Role.Prosumer, IsActive = false, AccountStatus = AccountStatus.Suspended }
                });

            var dashboard = await CreateService().BuildAsync();

            Assert.Equal(3, dashboard.RoleDistribution.Count);

            var prosumer = dashboard.RoleDistribution.Single(r => r.Role == nameof(Role.Prosumer));
            Assert.Equal(2, prosumer.UserCount);
            Assert.Equal(1, prosumer.ActiveUserCount);
            Assert.False(string.IsNullOrWhiteSpace(prosumer.Description));

            var operatorRole = dashboard.RoleDistribution.Single(r => r.Role == nameof(Role.MicrogridOperator));
            Assert.Equal(0, operatorRole.UserCount);
        }

        [Fact]
        public async Task BuildAsync_DegradesWhenAMonitorThrows()
        {
            _users.Setup(x => x.GetAllAsync(null, null, null, null))
                .ReturnsAsync(new List<User>());

            // M1 is unreachable: the dashboard must still render, flagging M1 as
            // unavailable rather than failing the whole page.
            _microgrids.Setup(x => x.GetCountAsync())
                .ThrowsAsync(new InvalidOperationException("M1 offline"));

            var dashboard = await CreateService().BuildAsync();

            Assert.Equal(DataSourceStatus.Unavailable, dashboard.Platform.Microgrids.Status);
            Assert.Equal(0, dashboard.Platform.MicrogridCount);

            // The healthy components are unaffected.
            Assert.Equal(DataSourceStatus.Available, dashboard.Platform.Reservations.Status);
            Assert.Equal(12, dashboard.Platform.ReservationCount);
        }

        [Fact]
        public async Task BuildAsync_DegradesWhenTheHealthCheckThrows()
        {
            // The health and activity inputs belong to M4 itself, not to a monitor
            // that already swallows errors, so they must be guarded here too.
            _users.Setup(x => x.GetAllAsync(null, null, null, null))
                .ReturnsAsync(new List<User> { new User { Id = "1", Role = Role.Admin, IsActive = true } });

            _health.Setup(x => x.GetHealthAsync())
                .ThrowsAsync(new TimeoutException("health check timed out"));
            _activity.Setup(x => x.GetRecentAsync(It.IsAny<int>()))
                .ThrowsAsync(new TimeoutException("activity read timed out"));
            _activity.Setup(x => x.CountAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ThrowsAsync(new TimeoutException("activity count timed out"));

            var dashboard = await CreateService().BuildAsync();

            Assert.Equal("Unknown", dashboard.Health.Api);
            Assert.Empty(dashboard.Activity.RecentActivity);
            Assert.Equal(0, dashboard.Activity.TotalEvents);
            // User-facing sections still render.
            Assert.Equal(1, dashboard.Users.TotalUsers);
            Assert.Equal(4, dashboard.Platform.MicrogridCount);
        }

        [Fact]
        public async Task BuildAsync_SurfacesMaintenanceModeFromConfiguration()
        {
            _users.Setup(x => x.GetAllAsync(null, null, null, null))
                .ReturnsAsync(new List<User>());

            _configuration.Setup(x => x.GetOrCreateAsync())
                .ReturnsAsync(new SystemConfiguration
                {
                    PlatformName = "Smart Microgrid",
                    MaintenanceMode = true,
                    MaintenanceMessage = "Scheduled maintenance."
                });

            var dashboard = await CreateService().BuildAsync();

            Assert.True(dashboard.MaintenanceMode);
        }
    }
}
