// ===========================================================================================================
// File: UserAccountStatusTests.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Unit/Integration test suite verifying UserAccountStatus operations and validations.
// ===========================================================================================================
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
        /// <summary>
        /// Initializes a new instance of the UserAccountStatusTests class.
        /// </summary>

        public UserAccountStatusTests()
        {
            // Initialize dependencies and state
            _users.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(true);
            _service = new UserService(_users.Object, _audit.Object);
        }
        /// <summary>
        /// Performs given user operation.
        /// </summary>

        private void GivenUser(User user) =>
            _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        /// <summary>
        /// Updates the specified account status async_suspending_mirrors is active and stamps audit record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_Suspending_MirrorsIsActiveAndStampsAudit()
        {
            // Execute update account status async_suspending_mirrors is active and stamps audit operations
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
        /// <summary>
        /// Updates the specified account status async_reactivating suspended user_is logged as reactivated record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_ReactivatingSuspendedUser_IsLoggedAsReactivated()
        {
            // Execute update account status async_reactivating suspended user_is logged as reactivated operations
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
        /// <summary>
        /// Updates the specified account status async_legacy inactive user_activates record.
        /// </summary>

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
        /// <summary>
        /// Updates the specified account status async_deactivating via legacy boolean endpoint_maps to inactive record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_DeactivatingViaLegacyBooleanEndpoint_MapsToInactive()
        {
            // Execute update account status async_deactivating via legacy boolean endpoint_maps to inactive operations
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
        /// <summary>
        /// Updates the specified account status async_rejects suspending the last active admin record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_RejectsSuspendingTheLastActiveAdmin()
        {
            // Execute update account status async_rejects suspending the last active admin operations
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
        /// <summary>
        /// Updates the specified account status async_allows suspending an admin when another remains record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_AllowsSuspendingAnAdminWhenAnotherRemains()
        {
            // Execute update account status async_allows suspending an admin when another remains operations
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
        /// <summary>
        /// Updates the specified role async_demoting the last admin_is refused record.
        /// </summary>

        [Fact]
        public async Task UpdateRoleAsync_DemotingTheLastAdmin_IsRefused()
        {
            // Execute update role async_demoting the last admin_is refused operations
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
        /// <summary>
        /// Updates the specified role async_demoting one admin when another remains_is allowed record.
        /// </summary>

        [Fact]
        public async Task UpdateRoleAsync_DemotingOneAdminWhenAnotherRemains_IsAllowed()
        {
            // Execute update role async_demoting one admin when another remains_is allowed operations
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
        /// <summary>
        /// Updates the specified role async_changing anon admin role_is not guarded record.
        /// </summary>

        [Fact]
        public async Task UpdateRoleAsync_ChangingANonAdminRole_IsNotGuarded()
        {
            // Execute update role async_changing anon admin role_is not guarded operations
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
        /// <summary>
        /// Deletes or removes the designated user async_deleting the last admin_is refused record.
        /// </summary>

        [Fact]
        public async Task DeleteUserAsync_DeletingTheLastAdmin_IsRefused()
        {
            // Execute delete user async_deleting the last admin_is refused operations
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
        /// <summary>
        /// Deletes or removes the designated user async_deleting anon admin_is allowed record.
        /// </summary>

        [Fact]
        public async Task DeleteUserAsync_DeletingANonAdmin_IsAllowed()
        {
            // Execute delete user async_deleting anon admin_is allowed operations
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
        /// <summary>
        /// Updates the specified account status async_same status_is ano op record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_SameStatus_IsANoOp()
        {
            // Execute update account status async_same status_is ano op operations
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
        /// <summary>
        /// Updates the specified account status async_unknown user_fails record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_UnknownUser_Fails()
        {
            // Execute update account status async_unknown user_fails operations
            _users.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((User?)null);

            var result = await _service.UpdateAccountStatusAsync("missing", AccountStatus.Suspended);

            Assert.False(result.Success);
            Assert.Equal("User not found.", result.Message);
        }
        /// <summary>
        /// Updates the specified account status async_pending status_mirrors is active record.
        /// </summary>

        [Fact]
        public async Task UpdateAccountStatusAsync_PendingStatus_MirrorsIsActive()
        {
            // Execute update account status async_pending status_mirrors is active operations
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

        /// <summary>
        /// Verifies that DeactivateSelfAsync deactivates account, sets Inactive, and records audit log.
        /// </summary>
        [Fact]
        public async Task DeactivateSelfAsync_ValidUser_SetsInactiveAndAudits()
        {
            var user = new User
            {
                Id = "u7",
                Email = "prosumer@smartmicrogrid.lk",
                FirstName = "Kasun",
                LastName = "Silva",
                Role = Role.Prosumer,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(user);

            var result = await _service.DeactivateSelfAsync("u7", "No longer using solar panels");

            Assert.True(result.Success);
            Assert.False(result.Data!.IsActive);
            Assert.Equal("Inactive", result.Data.AccountStatus);
            Assert.False(user.IsActive);
            Assert.Equal(AccountStatus.Inactive, user.AccountStatus);
            Assert.Equal("Kasun Silva (Self)", user.StatusChangedBy);
            _users.Verify(x => x.UpdateAsync(user), Times.Once);
            _audit.Verify(x => x.RecordAsync(
                AuditAction.UserDeactivated,
                AuditModule.UserManagement,
                It.Is<string>(d => d.Contains("prosumer@smartmicrogrid.lk") && d.Contains("No longer using solar panels")),
                "u7",
                "Kasun Silva",
                "Prosumer",
                "User",
                "u7",
                It.IsAny<string?>(),
                It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Verifies that DeactivateSelfAsync fails when the last active admin attempts to deactivate their own account.
        /// </summary>
        [Fact]
        public async Task DeactivateSelfAsync_LastAdmin_ReturnsFailure()
        {
            var admin = new User
            {
                Id = "admin1",
                Email = "admin@smartmicrogrid.lk",
                FirstName = "Admin",
                LastName = "User",
                Role = Role.Admin,
                IsActive = true,
                AccountStatus = AccountStatus.Active
            };
            GivenUser(admin);
            _users.Setup(x => x.GetAllAsync(null, Role.Admin, true, null))
                .ReturnsAsync(new List<User> { admin });

            var result = await _service.DeactivateSelfAsync("admin1");

            Assert.False(result.Success);
            Assert.Contains("cannot be deactivated", result.Message);
            Assert.True(admin.IsActive);
            _users.Verify(x => x.UpdateAsync(admin), Times.Never);
        }

        /// <summary>
        /// Verifies that GetUserByNicAsync retrieves prosumer by NIC natural key.
        /// </summary>
        [Fact]
        public async Task GetUserByNicAsync_ExistingNic_ReturnsUser()
        {
            var user = new User
            {
                Id = "u8",
                Nic = "200012345678",
                Email = "prosumer_nic@smartmicrogrid.lk",
                Role = Role.Prosumer,
                IsActive = true
            };
            _users.Setup(x => x.GetByNicAsync("200012345678")).ReturnsAsync(user);

            var result = await _service.GetUserByNicAsync("200012345678");

            Assert.True(result.Success);
            Assert.Equal("200012345678", result.Data!.Nic);
            Assert.Equal("u8", result.Data.Id);
        }

        /// <summary>
        /// Verifies that GetUserByNicAsync fails when NIC is not found.
        /// </summary>
        [Fact]
        public async Task GetUserByNicAsync_NonExistingNic_ReturnsFailure()
        {
            _users.Setup(x => x.GetByNicAsync("NONEXISTENT")).ReturnsAsync((User?)null);

            var result = await _service.GetUserByNicAsync("NONEXISTENT");

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }
    }
}
