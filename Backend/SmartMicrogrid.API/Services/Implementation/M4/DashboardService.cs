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
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISystemActivityRepository _activityRepository;
        private readonly ISystemConfigurationRepository _configurationRepository;
        private readonly IMicrogridMonitorService _microgridMonitor;
        private readonly IReservationMonitorService _reservationMonitor;
        private readonly ITransactionMonitorService _transactionMonitor;
        private readonly ISystemHealthService _healthService;

        public DashboardService(
            IUserRepository userRepository,
            ISystemActivityRepository activityRepository,
            ISystemConfigurationRepository configurationRepository,
            IMicrogridMonitorService microgridMonitor,
            IReservationMonitorService reservationMonitor,
            ITransactionMonitorService transactionMonitor,
            ISystemHealthService healthService)
        {
            _userRepository = userRepository;
            _activityRepository = activityRepository;
            _configurationRepository = configurationRepository;
            _microgridMonitor = microgridMonitor;
            _reservationMonitor = reservationMonitor;
            _transactionMonitor = transactionMonitor;
            _healthService = healthService;
        }

        public async Task<AdminDashboardDto> BuildAsync()
        {
            var configuration = await _configurationRepository.GetOrCreateAsync();

            // M1/M2/M3 are optional inputs. Gather them in parallel and let each one
            // degrade independently so a broken component cannot break the dashboard.
            // Each fetch is individually guarded: the monitor implementations already
            // swallow their own failures, but health and activity come from M4 itself,
            // and a failing health check must never take down the health page.
            var microgridCountTask = SafeStatAsync(() => _microgridMonitor.GetCountAsync(), "M1");
            var capacityTask = SafeStatAsync(() => _microgridMonitor.GetCapacitySummaryAsync(), "M1");
            var reservationCountTask = SafeStatAsync(() => _reservationMonitor.GetCountAsync(), "M2");
            var pendingReservationTask = SafeStatAsync(() => _reservationMonitor.GetPendingCountAsync(), "M2");
            var transactionCountTask = SafeStatAsync(() => _transactionMonitor.GetCountAsync(), "M3");
            var completedTransactionTask = SafeStatAsync(() => _transactionMonitor.GetCompletedCountAsync(), "M3");
            var healthTask = SafeAsync(() => _healthService.GetHealthAsync(), UnavailableHealth);
            var recentActivityTask = SafeAsync(() => _activityRepository.GetRecentAsync(10), () => new List<SystemActivity>());

            await Task.WhenAll(
                microgridCountTask, capacityTask, reservationCountTask,
                pendingReservationTask, transactionCountTask, completedTransactionTask,
                healthTask, recentActivityTask);

            var users = (await _userRepository.GetAllAsync()).ToList();
            var eventCount = await SafeAsync(() => _activityRepository.CountAsync(), () => 0L);

            var microgridCount = microgridCountTask.Result;
            var capacity = capacityTask.Result;
            var reservationCount = reservationCountTask.Result;
            var pendingReservation = pendingReservationTask.Result;
            var transactionCount = transactionCountTask.Result;
            var completedTransaction = completedTransactionTask.Result;

            return new AdminDashboardDto
            {
                PlatformName = configuration.PlatformName,
                GeneratedAt = DateTime.UtcNow,
                MaintenanceMode = configuration.MaintenanceMode,
                Users = BuildUserSummary(users),
                RoleDistribution = BuildRoleDistribution(users),
                Platform = new PlatformOverviewDto
                {
                    MicrogridCount = microgridCount.Count,
                    ActiveMicrogridCount = (long)microgridCount.Available,
                    TotalCapacity = capacity.Total,
                    TotalAvailableCapacity = capacity.Available,
                    ReservationCount = reservationCount.Count,
                    PendingReservationCount = pendingReservation.Count,
                    TransactionCount = transactionCount.Count,
                    CompletedTransactionCount = completedTransaction.Count,
                    Microgrids = microgridCount,
                    Reservations = reservationCount,
                    Transactions = transactionCount
                },
                Activity = new ActivitySummaryDto
                {
                    RecentActivity = recentActivityTask.Result
                        .Select(ActivityMapper.ToDto)
                        .ToArray(),
                    TotalEvents = eventCount
                },
                Health = healthTask.Result
            };
        }

        /// <summary>
        /// Runs an optional dashboard input and converts a failure into the
        /// supplied fallback so one broken component cannot blank the page.
        /// </summary>
        private static async Task<T> SafeAsync<T>(Func<Task<T>> fetch, Func<T> fallback)
        {
            try
            {
                return await fetch();
            }
            catch
            {
                return fallback();
            }
        }

        private static Task<ComponentStatDto> SafeStatAsync(Func<Task<ComponentStatDto>> fetch, string component) =>
            SafeAsync(fetch, () => new ComponentStatDto
            {
                Status = DataSourceStatus.Unavailable,
                Note = $"{component} data is not reachable."
            });

        private static SystemHealthDto UnavailableHealth() => new()
        {
            Api = "Unknown",
            Database = "Unknown",
            DatabaseDetail = "Health check could not be completed.",
            Server = "Online",
            ServerTime = DateTime.UtcNow
        };

        private static DashboardSummaryDto BuildUserSummary(List<User> users) => new()
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Active),
            InactiveUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Inactive),
            SuspendedUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Suspended),
            PendingUsers = users.Count(u => ResolveStatus(u) == AccountStatus.Pending)
        };

        private static List<RoleDistributionDto> BuildRoleDistribution(List<User> users) =>
            Enum.GetValues<Role>()
                .Select(role => new RoleDistributionDto
                {
                    Role = role.ToString(),
                    Description = RoleDescriptions.Describe(role),
                    UserCount = users.Count(u => u.Role == role),
                    ActiveUserCount = users.Count(u =>
                        u.Role == role && ResolveStatus(u) == AccountStatus.Active)
                })
                .ToList();

        /// <summary>
        /// Documents written before AccountStatus existed have no value, so they
        /// deserialize as Active. IsActive is the fallback for those same records.
        /// </summary>
        internal static AccountStatus ResolveStatus(User user)
        {
            if (user.AccountStatus != AccountStatus.Active)
                return user.AccountStatus;

            return user.IsActive ? AccountStatus.Active : AccountStatus.Inactive;
        }
    }
}
