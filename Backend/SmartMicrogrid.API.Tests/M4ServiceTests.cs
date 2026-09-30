using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces.M4;
using SmartMicrogrid.API.Services.Implementation.M4;
using SmartMicrogrid.API.Services.Interfaces.M4;
using Xunit;

namespace SmartMicrogrid.API.Tests
{
    public class SystemConfigurationServiceTests
    {
        private readonly Mock<ISystemConfigurationRepository> _repository = new();
        private readonly Mock<IAuditService> _audit = new();
        private readonly SystemConfigurationService _service;

        public SystemConfigurationServiceTests()
        {
            _repository.Setup(x => x.GetOrCreateAsync())
                .ReturnsAsync(() => new SystemConfiguration
                {
                    Id = "system-config",
                    PlatformName = "Smart Microgrid",
                    AllowRegistration = true
                });
            _repository.Setup(x => x.UpsertAsync(It.IsAny<SystemConfiguration>()))
                .ReturnsAsync((SystemConfiguration value) => value);
            _service = new SystemConfigurationService(_repository.Object, _audit.Object);
        }

        [Fact]
        public async Task UpdateAsync_PersistsTrimmedValuesAndStampsTheOperator()
        {
            var result = await _service.UpdateAsync(new UpdateConfigurationDto
            {
                PlatformName = "  National Microgrid Platform  ",
                PlatformDescription = "  Pilot deployment.  ",
                MaintenanceMessage = "  Window on Friday  ",
                SessionTimeoutMinutes = 120,
                MaxLoginAttempts = 3,
                DefaultPageSize = 50
            }, "backoffice1");

            Assert.Equal("National Microgrid Platform", result.PlatformName);
            Assert.Equal("Pilot deployment.", result.PlatformDescription);
            Assert.Equal("Window on Friday", result.MaintenanceMessage);
            Assert.Equal(120, result.SessionTimeoutMinutes);
            Assert.Equal(3, result.MaxLoginAttempts);
            Assert.Equal(50, result.DefaultPageSize);
            Assert.Equal("backoffice1", result.UpdatedBy);

            _repository.Verify(x => x.UpsertAsync(It.IsAny<SystemConfiguration>()), Times.Once);
            _audit.Verify(x => x.RecordAsync(
                AuditAction.ConfigurationUpdated,
                AuditModule.SystemConfiguration,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                "backoffice1",
                It.IsAny<string?>(),
                "SystemConfiguration",
                "system-config",
                It.IsAny<string?>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_LeavesTheTogglesUntouched()
        {
            // Maintenance and registration have dedicated endpoints. A details
            // update must never silently flip them.
            _repository.Setup(x => x.GetOrCreateAsync())
                .ReturnsAsync(() => new SystemConfiguration
                {
                    PlatformName = "Smart Microgrid",
                    MaintenanceMode = true,
                    MaintenanceMessage = "Existing maintenance notice.",
                    AllowRegistration = false
                });

            var result = await _service.UpdateAsync(new UpdateConfigurationDto
            {
                PlatformName = "Renamed Platform",
                SessionTimeoutMinutes = 60,
                MaxLoginAttempts = 5,
                DefaultPageSize = 20
            }, "backoffice1");

            Assert.True(result.MaintenanceMode);
            Assert.Equal("Existing maintenance notice.", result.MaintenanceMessage);
            Assert.False(result.AllowRegistration);
        }

        [Fact]
        public async Task UpdateAsync_ExplicitEmptyMessage_ClearsTheBanner()
        {
            _repository.Setup(x => x.GetOrCreateAsync())
                .ReturnsAsync(() => new SystemConfiguration
                {
                    PlatformName = "Smart Microgrid",
                    MaintenanceMode = true,
                    MaintenanceMessage = "Existing maintenance notice."
                });

            var result = await _service.UpdateAsync(new UpdateConfigurationDto
            {
                PlatformName = "Smart Microgrid",
                MaintenanceMessage = "",
                SessionTimeoutMinutes = 60,
                MaxLoginAttempts = 5,
                DefaultPageSize = 20
            }, "backoffice1");

            Assert.Equal(string.Empty, result.MaintenanceMessage);
        }

        [Fact]
        public async Task SetMaintenanceModeAsync_StoringTheMessageOnlyWhileEnabled()
        {
            var enabled = await _service.SetMaintenanceModeAsync(
                new MaintenanceModeDto { MaintenanceMode = true, MaintenanceMessage = "  Grid upgrade  " },
                "backoffice1");

            Assert.True(enabled.MaintenanceMode);
            Assert.Equal("Grid upgrade", enabled.MaintenanceMessage);
            _audit.Verify(x => x.RecordAsync(
                AuditAction.ConfigurationUpdated,
                AuditModule.SystemConfiguration,
                "Maintenance mode enabled.",
                It.IsAny<string?>(),
                "backoffice1",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>()), Times.Once);

            var disabled = await _service.SetMaintenanceModeAsync(
                new MaintenanceModeDto { MaintenanceMode = false, MaintenanceMessage = "ignored" },
                "backoffice1");

            Assert.False(disabled.MaintenanceMode);
            // A stale banner must not survive once maintenance is switched off.
            Assert.Null(disabled.MaintenanceMessage);
        }

        [Fact]
        public async Task SetRegistrationModeAsync_UpdatesTheFlagAndAudits()
        {
            var result = await _service.SetRegistrationModeAsync(
                new RegistrationModeDto { AllowRegistration = false },
                "backoffice1");

            Assert.False(result.AllowRegistration);
            _audit.Verify(x => x.RecordAsync(
                AuditAction.ConfigurationUpdated,
                AuditModule.SystemConfiguration,
                "Public registration disabled.",
                It.IsAny<string?>(),
                "backoffice1",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>()), Times.Once);
        }
    }

    public class AuditServiceTests
    {
        private readonly Mock<ISystemActivityRepository> _repository = new();
        private readonly DefaultHttpContext _httpContext = new();

        public AuditServiceTests()
        {
            _repository.Setup(x => x.RecordAsync(It.IsAny<SystemActivity>()))
                .ReturnsAsync((SystemActivity value) => value);
        }

        private AuditService CreateService()
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(x => x.HttpContext).Returns(_httpContext);
            return new AuditService(_repository.Object, accessor.Object, NullLogger<AuditService>.Instance);
        }

        [Fact]
        public async Task RecordAsync_WithoutExplicitActor_ResolvesFromTheCurrentPrincipal()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin1"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.GivenName, "Grace"),
                new Claim(ClaimTypes.Surname, "Perera")
            }, "TestAuth");

            _httpContext.User = new ClaimsPrincipal(identity);
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.9");

            await CreateService().RecordAsync(
                AuditAction.UserCreated,
                AuditModule.UserManagement,
                "Created a prosumer account.");

            _repository.Verify(x => x.RecordAsync(It.Is<SystemActivity>(a =>
                a.UserId == "admin1" &&
                a.UserName == "Grace Perera" &&
                a.Role == "Admin" &&
                a.Action == AuditAction.UserCreated &&
                a.Module == AuditModule.UserManagement &&
                a.Status == AuditStatus.Success &&
                a.IpAddress == "10.0.0.9" &&
                a.Timestamp != default)), Times.Once);
        }

        [Fact]
        public async Task RecordAsync_UsesTheFirstForwardedAddressWhenProxied()
        {
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"));
            _httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.7, 10.0.0.1";

            await CreateService().RecordAsync(
                AuditAction.ConfigurationUpdated,
                AuditModule.SystemConfiguration,
                "Configuration saved.");

            _repository.Verify(x => x.RecordAsync(It.Is<SystemActivity>(a =>
                a.IpAddress == "203.0.113.7")), Times.Once);
        }

        [Fact]
        public async Task RecordAsync_AnonymousRequest_IsLabelledAnonymous()
        {
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            await CreateService().RecordAsync(
                AuditAction.LoginFailed,
                AuditModule.Authentication,
                "Login attempt failed.");

            _repository.Verify(x => x.RecordAsync(It.Is<SystemActivity>(a =>
                a.UserName == "Anonymous" && a.UserId == null)), Times.Once);
        }

        [Fact]
        public async Task RecordAsync_RepositoryFailure_DoesNotPropagate()
        {
            // Auditing must never take down the business operation it is recording.
            _repository.Setup(x => x.RecordAsync(It.IsAny<SystemActivity>()))
                .ThrowsAsync(new InvalidOperationException("audit collection unavailable"));

            await CreateService().RecordAsync(
                AuditAction.LoginSuccess,
                AuditModule.Authentication,
                "Signed in.");
        }

        [Fact]
        public async Task RecordAsync_ExplicitActorWinsOverThePrincipal()
        {
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin1"),
                new Claim(ClaimTypes.GivenName, "Grace")
            }, "TestAuth"));

            await CreateService().RecordAsync(
                AuditAction.LoginFailed,
                AuditModule.Authentication,
                "Bad password.",
                userId: "someone-else",
                userName: "Unknown User",
                role: "Prosumer",
                entityType: "User",
                entityId: "u9",
                ipAddress: "198.51.100.4",
                status: AuditStatus.Failure);

            _repository.Verify(x => x.RecordAsync(It.Is<SystemActivity>(a =>
                a.UserId == "someone-else" &&
                a.UserName == "Unknown User" &&
                a.Role == "Prosumer" &&
                a.EntityType == "User" &&
                a.EntityId == "u9" &&
                a.IpAddress == "198.51.100.4" &&
                a.Status == AuditStatus.Failure)), Times.Once);
        }
    }
}
