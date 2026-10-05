// ===========================================================================================================
// File: ReportService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Business logic service implementing ReportService operations, rules, and workflows.
// ===========================================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Repositories.Interfaces.M4;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation.M4
{
    public class ReportService : IReportService
    {
        private const int RecentActivitySampleSize = 50;

        private readonly IUserRepository _userRepository;
        private readonly ISystemActivityRepository _activityRepository;
        private readonly ISystemConfigurationRepository _configurationRepository;
        private readonly IMicrogridMonitorService _microgridMonitor;
        private readonly IReservationMonitorService _reservationMonitor;
        private readonly ITransactionMonitorService _transactionMonitor;
        private readonly IAuditService _auditService;
        /// <summary>
        /// Initializes a new instance of the ReportService class.
        /// </summary>

        public ReportService(
            IUserRepository userRepository,
            ISystemActivityRepository activityRepository,
            ISystemConfigurationRepository configurationRepository,
            IMicrogridMonitorService microgridMonitor,
            IReservationMonitorService reservationMonitor,
            ITransactionMonitorService transactionMonitor,
            IAuditService auditService)
        {
            // Initialize dependencies and state
            _userRepository = userRepository;
            _activityRepository = activityRepository;
            _configurationRepository = configurationRepository;
            _microgridMonitor = microgridMonitor;
            _reservationMonitor = reservationMonitor;
            _transactionMonitor = transactionMonitor;
            _auditService = auditService;
        }
        /// <summary>
        /// Retrieves user report async details.
        /// </summary>

        public async Task<UserReportDto> GetUserReportAsync(DateTime? from, DateTime? to, string? role, string? status)
        {
            // Execute get user report async operations
            ValidateRange(from, to);

            var users = (await _userRepository.GetAllAsync()).ToList();

            var scoped = users
                .Where(u => string.IsNullOrWhiteSpace(role)
                            || string.Equals(u.Role.ToString(), role.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(u => string.IsNullOrWhiteSpace(status)
                            || string.Equals(ResolveStatus(u).ToString(), status.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            var report = new UserReportDto
            {
                From = from,
                To = to,
                TotalUsers = scoped.Count,
                ActiveUsers = scoped.Count(u => ResolveStatus(u) == AccountStatus.Active),
                InactiveUsers = scoped.Count(u => ResolveStatus(u) == AccountStatus.Inactive),
                SuspendedUsers = scoped.Count(u => ResolveStatus(u) == AccountStatus.Suspended),
                PendingUsers = scoped.Count(u => ResolveStatus(u) == AccountStatus.Pending),
                NewUsersInPeriod = scoped.Count(u => u.CreatedAt >= (from ?? DateTime.MinValue)
                                                    && u.CreatedAt <= (to ?? DateTime.MaxValue)),
                ByStatus = Enum.GetValues<AccountStatus>()
                    .Select(s => new UserSummaryRowDto
                    {
                        Role = "-",
                        Status = s.ToString(),
                        Count = scoped.Count(u => ResolveStatus(u) == s)
                    })
                    .Where(r => r.Count > 0)
                    .ToList(),
                ByRole = Enum.GetValues<Role>()
                    .Select(r => new RoleDistributionDto
                    {
                        Role = r.ToString(),
                        Description = RoleDescriptions.Describe(r),
                        UserCount = scoped.Count(u => u.Role == r),
                        ActiveUserCount = scoped.Count(u =>
                            u.Role == r && ResolveStatus(u) == AccountStatus.Active)
                    })
                    .Where(r => r.UserCount > 0)
                    .ToList()
            };

            await _auditService.RecordAsync(
                AuditAction.ReportGenerated,
                AuditModule.Reporting,
                "User summary report generated.");

            return report;
        }
        /// <summary>
        /// Retrieves role report async details.
        /// </summary>

        public async Task<List<RoleDistributionDto>> GetRoleReportAsync()
        {
            // Execute get role report async operations
            var users = (await _userRepository.GetAllAsync()).ToList();

            var report = Enum.GetValues<Role>()
                .Select(role => new RoleDistributionDto
                {
                    Role = role.ToString(),
                    Description = RoleDescriptions.Describe(role),
                    UserCount = users.Count(u => u.Role == role),
                    ActiveUserCount = users.Count(u =>
                        u.Role == role && ResolveStatus(u) == AccountStatus.Active)
                })
                .ToList();

            await _auditService.RecordAsync(
                AuditAction.ReportGenerated,
                AuditModule.Reporting,
                "Role distribution report generated.");

            return report;
        }
        /// <summary>
        /// Retrieves activity report async details.
        /// </summary>

        public async Task<ActivityReportDto> GetActivityReportAsync(DateTime? from, DateTime? to)
        {
            // Execute get activity report async operations
            ValidateRange(from, to);

            var byAction = await _activityRepository.GetActionCountsAsync(from, to);
            var byModule = await _activityRepository.GetModuleCountsAsync(from, to);
            var recent = await _activityRepository.GetRecentAsync(RecentActivitySampleSize);

            var successEvents = await CountByStatusAsync(AuditStatus.Success, from, to);
            var failureEvents = await CountByStatusAsync(AuditStatus.Failure, from, to);

            var report = new ActivityReportDto
            {
                TotalEvents = byAction.Values.Sum(),
                SuccessEvents = successEvents,
                FailureEvents = failureEvents,
                ByAction = byAction,
                ByModule = byModule,
                Recent = recent.Select(ActivityMapper.ToDto).ToList()
            };

            await _auditService.RecordAsync(
                AuditAction.ReportGenerated,
                AuditModule.Reporting,
                $"System activity report generated ({report.TotalEvents} events in range).");

            return report;
        }
        /// <summary>
        /// Retrieves platform report async details.
        /// </summary>

        public async Task<PlatformReportDto> GetPlatformReportAsync(DateTime? from, DateTime? to)
        {
            // Execute get platform report async operations
            ValidateRange(from, to);

            var users = (await _userRepository.GetAllAsync()).ToList();

            var microgridsTask = _microgridMonitor.GetCountAsync();
            var capacityTask = _microgridMonitor.GetCapacitySummaryAsync();
            var reservationsTask = _reservationMonitor.GetCountAsync();
            var pendingReservationsTask = _reservationMonitor.GetPendingCountAsync();
            var transactionsTask = _transactionMonitor.GetCountAsync();
            var completedTransactionsTask = _transactionMonitor.GetCompletedCountAsync();
            var activityTask = GetActivityReportAsync(from, to);

            await Task.WhenAll(
                microgridsTask, capacityTask, reservationsTask, pendingReservationsTask,
                transactionsTask, completedTransactionsTask, activityTask);

            var configuration = await _configurationRepository.GetOrCreateAsync();

            return new PlatformReportDto
            {
                From = from,
                To = to,
                Users = new DashboardSummaryDto
                {
                    TotalUsers = users.Count,
                    ActiveUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Active),
                    InactiveUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Inactive),
                    SuspendedUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Suspended),
                    PendingUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Pending)
                },
                RoleDistribution = Enum.GetValues<Role>()
                    .Select(role => new RoleDistributionDto
                    {
                        Role = role.ToString(),
                        Description = RoleDescriptions.Describe(role),
                        UserCount = users.Count(u => u.Role == role),
                        ActiveUserCount = users.Count(u =>
                            u.Role == role && ResolveStatus(u) == AccountStatus.Active)
                    })
                    .ToList(),
                Platform = new PlatformOverviewDto
                {
                    MicrogridCount = microgridsTask.Result.Count,
                    ActiveMicrogridCount = (long)microgridsTask.Result.Available,
                    TotalCapacity = capacityTask.Result.Total,
                    TotalAvailableCapacity = capacityTask.Result.Available,
                    ReservationCount = reservationsTask.Result.Count,
                    PendingReservationCount = pendingReservationsTask.Result.Count,
                    TransactionCount = transactionsTask.Result.Count,
                    CompletedTransactionCount = completedTransactionsTask.Result.Count,
                    Microgrids = microgridsTask.Result,
                    Reservations = reservationsTask.Result,
                    Transactions = transactionsTask.Result
                },
                Activity = activityTask.Result
            };
        }
        /// <summary>
        /// Performs count by status async operation.
        /// </summary>

        private async Task<long> CountByStatusAsync(string status, DateTime? from, DateTime? to)
        {
            // Execute count by status async operations
            return await _activityRepository.CountAsync(null, null, null, status, from, to);
        }
        /// <summary>
        /// Verifies and validates te range criteria.
        /// </summary>

        private static void ValidateRange(DateTime? from, DateTime? to)
        {
            // Execute validate range operations
            if (from.HasValue && to.HasValue && from > to)
            {
                throw new ArgumentException("The 'from' date must not be later than the 'to' date.");
            }
        }
        /// <summary>
        /// Performs resolve status operation.
        /// </summary>

        private static AccountStatus ResolveStatus(User user) => DashboardService.ResolveStatus(user);
    }
}
