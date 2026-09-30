using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using SmartMicrogrid.API.DTOs.Users;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Implementation;
using SmartMicrogrid.API.Services.Interfaces.M4;
using Xunit;

namespace SmartMicrogrid.API.Tests
{
    public class UserAccountStatusTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IAuditService> _audit = new();
        private readonly UserService _service;

        public UserAccountStatusTests()
        {
            _users.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(true);
            _service = new UserService(_users.Object, _audit.Object);
        }

        private void GivenUser(User user) =>
            _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);

        [Fact]
        public async Task UpdateAccountStatusAsync_Suspending_MirrorsIsActiveAndStampsAudit()
        {
            var user = new User
            {
                Id = "u1",
                Email = "grace@smartmicrogrid.lk",
                FirstName = "Grace",
                LastName = "Perera",
                Role = Role.Prosumer,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(user);

            var result = await _service.UpdateAccountStatusAsync("u1", AccountStatus.Suspended);

            Assert.True(result.Success);
            Assert.Equal("Suspended", result.Data!.AccountStatus);
            Assert.False(result.Data.IsActive);
            Assert.Equal(AccountStatus.Suspended, user.AccountStatus);
            Assert.False(user.IsActive);
            Assert.NotNull(user.StatusChangedAt);

            // The actor is left unresolved on purpose so the audit service records
            // the administrator who performed the change, not the target account.
            // The target is identified by entityId and named in the description.
            _audit.Verify(x => x.RecordAsync(
                AuditAction.UserSuspended,
                AuditModule.UserManagement,
                It.Is<string>(d => d.Contains("Active")
                    && d.Contains("Suspended")
                    && d.Contains("grace@smartmicrogrid.lk")
                    && d.Contains("Grace Perera")),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                "User",
                "u1",
                It.IsAny<string?>(),
                It.IsAny<string>()), Times.Once);

            _audit.Verify(x => x.RecordAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                "u1", It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_ReactivatingSuspendedUser_IsLoggedAsReactivated()
        {
            var user = new User
            {
                Id = "u2",
                Email = "sam@smartmicrogrid.lk",
                FirstName = "Sam",
                LastName = "Perera",
                Role = Role.Prosumer,
                IsActive = false,
                AccountStatus = AccountStatus.Suspended
            };
            GivenUser(user);

            var result = await _service.UpdateAccountStatusAsync("u2", AccountStatus.Active);

            Assert.True(result.Success);
            Assert.True(result.Data!.IsActive);
            Assert.Equal(AccountStatus.Active, user.AccountStatus);
            _audit.Verify(x => x.RecordAsync(
                AuditAction.UserReactivated,
                AuditModule.UserManagement,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_LegacyInactiveUser_Activates()
        {
            // AccountStatus defaults to Active on documents written before the
            // field existed, so IsActive is the source of truth for the transition.
            var user = new User
            {
                Id = "u3",
                Email = "old@smartmicrogrid.lk",
                Role = Role.MicrogridOperator,
                IsActive = false
            };
            GivenUser(user);

            var result = await _service.UpdateAccountStatusAsync("u3", AccountStatus.Active);

            Assert.True(result.Success);
            Assert.True(result.Data!.IsActive);
            _audit.Verify(x => x.RecordAsync(
                AuditAction.UserActivated,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_DeactivatingViaLegacyBooleanEndpoint_MapsToInactive()
        {
            var user = new User
            {
                Id = "u4",
                Email = "deact@smartmicrogrid.lk",
                Role = Role.Prosumer,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(user);

            var result = await _service.UpdateStatusAsync("u4", false);

            Assert.True(result.Success);
            Assert.Equal("Inactive", result.Data!.AccountStatus);
            Assert.False(result.Data.IsActive);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_RejectsSuspendingTheLastActiveAdmin()
        {
            var admin = new User
            {
                Id = "admin1",
                Email = "admin@smartmicrogrid.lk",
                Role = Role.Admin,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(admin);
            // He is the only active Backoffice officer.
            _users.Setup(x => x.GetAllAsync(null, Role.Admin, true, null))
                .ReturnsAsync(new List<User> { admin });

            var result = await _service.UpdateAccountStatusAsync("admin1", AccountStatus.Suspended);

            Assert.False(result.Success);
            Assert.Contains("last active Backoffice officer", result.Message);
            Assert.True(admin.IsActive);
            Assert.Equal(AccountStatus.Active, admin.AccountStatus);
            _users.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
            _audit.Verify(x => x.RecordAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_AllowsSuspendingAnAdminWhenAnotherRemains()
        {
            var admin = new User
            {
                Id = "admin2",
                Email = "admin2@smartmicrogrid.lk",
                FirstName = "Nimal",
                LastName = "Silva",
                Role = Role.Admin,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(admin);
            _users.Setup(x => x.GetAllAsync(null, Role.Admin, true, null))
                .ReturnsAsync(new List<User>
                {
                    admin,
                    new User { Id = "admin1", Email = "admin1@smartmicrogrid.lk", Role = Role.Admin, IsActive = true }
                });

            var result = await _service.UpdateAccountStatusAsync("admin2", AccountStatus.Suspended);

            Assert.True(result.Success);
            Assert.Equal("Suspended", result.Data!.AccountStatus);
            Assert.False(admin.IsActive);
        }

        [Fact]
        public async Task UpdateRoleAsync_DemotingTheLastAdmin_IsRefused()
        {
            var admin = new User
            {
                Id = "admin1",
                Email = "admin1@smartmicrogrid.lk",
                FirstName = "Nimal",
                LastName = "Silva",
                Role = Role.Admin,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(admin);
            _users.Setup(x => x.GetAllAsync(null, Role.Admin, true, null))
                .ReturnsAsync(new List<User> { admin });

            var result = await _service.UpdateRoleAsync("admin1", Role.Prosumer);

            Assert.False(result.Success);
            Assert.Contains("last active Backoffice officer", result.Message);
            Assert.Equal(Role.Admin, admin.Role);
            _users.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRoleAsync_DemotingOneAdminWhenAnotherRemains_IsAllowed()
        {
            var admin = new User
            {
                Id = "admin2",
                Email = "admin2@smartmicrogrid.lk",
                Role = Role.Admin,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(admin);
            _users.Setup(x => x.GetAllAsync(null, Role.Admin, true, null))
                .ReturnsAsync(new List<User>
                {
                    admin,
                    new User { Id = "admin1", Email = "admin1@smartmicrogrid.lk", Role = Role.Admin, IsActive = true }
                });

            var result = await _service.UpdateRoleAsync("admin2", Role.MicrogridOperator);

            Assert.True(result.Success);
            Assert.Equal(Role.MicrogridOperator, admin.Role);
        }

        [Fact]
        public async Task UpdateRoleAsync_ChangingANonAdminRole_IsNotGuarded()
        {
            var user = new User
            {
                Id = "u9",
                Email = "grace@smartmicrogrid.lk",
                Role = Role.Prosumer,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(user);

            var result = await _service.UpdateRoleAsync("u9", Role.MicrogridOperator);

            Assert.True(result.Success);
            Assert.Equal(Role.MicrogridOperator, user.Role);
            _users.Verify(x => x.GetAllAsync(null, Role.Admin, true, null), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_DeletingTheLastAdmin_IsRefused()
        {
            var admin = new User
            {
                Id = "admin1",
                Email = "admin1@smartmicrogrid.lk",
                Role = Role.Admin,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(admin);
            _users.Setup(x => x.GetAllAsync(null, Role.Admin, true, null))
                .ReturnsAsync(new List<User> { admin });

            var result = await _service.DeleteUserAsync("admin1");

            Assert.False(result.Success);
            Assert.Contains("last active Backoffice officer", result.Message);
            _users.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_DeletingANonAdmin_IsAllowed()
        {
            var user = new User
            {
                Id = "u9",
                Email = "grace@smartmicrogrid.lk",
                Role = Role.Prosumer,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(user);
            _users.Setup(x => x.DeleteAsync("u9")).ReturnsAsync(true);

            var result = await _service.DeleteUserAsync("u9");

            Assert.True(result.Success);
            _users.Verify(x => x.DeleteAsync("u9"), Times.Once);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_SameStatus_IsANoOp()
        {
            var user = new User
            {
                Id = "u5",
                Email = "noop@smartmicrogrid.lk",
                Role = Role.Prosumer,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(user);

            var result = await _service.UpdateAccountStatusAsync("u5", AccountStatus.Active);

            Assert.True(result.Success);
            Assert.Contains("already Active", result.Message);
            _users.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_UnknownUser_Fails()
        {
            _users.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((User?)null);

            var result = await _service.UpdateAccountStatusAsync("missing", AccountStatus.Suspended);

            Assert.False(result.Success);
            Assert.Equal("User not found.", result.Message);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_PendingStatus_MirrorsIsActive()
        {
            var user = new User
            {
                Id = "u6",
                Email = "pending@smartmicrogrid.lk",
                Role = Role.Prosumer,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(user);

            var result = await _service.UpdateAccountStatusAsync("u6", AccountStatus.Pending);

            Assert.True(result.Success);
            Assert.Equal("Pending", result.Data!.AccountStatus);
            Assert.False(result.Data.IsActive);
        }
    }
}
