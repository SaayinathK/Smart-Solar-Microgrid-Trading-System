// ===========================================================================================================
// File: DashboardDto.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Data transfer object (DTO) representing DashboardDto communication payload.
// ===========================================================================================================
using System;
using System.Collections.Generic;

namespace SmartMicrogrid.API.DTOs.M4
{
    /// <summary>
    /// A single M1/M2/M3 statistic as surfaced to the admin dashboard. Source is
    /// "Live" when the owning component answered and "Unavailable" when it did
    /// not, so the dashboard degrades instead of failing.
    /// </summary>
    public class ComponentStatDto
    {
        public long Count { get; set; }
        public double Total { get; set; }
        public double Available { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = DataSourceStatus.Available;
        public string Note { get; set; } = string.Empty;
    }

    public static class DataSourceStatus
    {
        public const string Available = "Live";
        public const string Unavailable = "Unavailable";
    }
    public class DashboardSummaryDto
    {
        public long TotalUsers { get; set; }
        public long ActiveUsers { get; set; }
        public long InactiveUsers { get; set; }
        public long SuspendedUsers { get; set; }
        public long PendingUsers { get; set; }
    }

    public class RoleDistributionDto
    {
        public string Role { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long UserCount { get; set; }
        public long ActiveUserCount { get; set; }
    }

    public class PlatformOverviewDto
    {
        public long MicrogridCount { get; set; }
        public long ActiveMicrogridCount { get; set; }
        public double TotalCapacity { get; set; }
        public double TotalAvailableCapacity { get; set; }

        public long ReservationCount { get; set; }
        public long PendingReservationCount { get; set; }

        public long TransactionCount { get; set; }
        public long CompletedTransactionCount { get; set; }

        public ComponentStatDto Microgrids { get; set; } = new();
        public ComponentStatDto Reservations { get; set; } = new();
        public ComponentStatDto Transactions { get; set; } = new();
    }

    public class ActivitySummaryDto
    {
        public SystemActivityDto[] RecentActivity { get; set; } = Array.Empty<SystemActivityDto>();
        public long TotalEvents { get; set; }
    }

    public class AdminDashboardDto
    {
        public string PlatformName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public bool MaintenanceMode { get; set; }

        public DashboardSummaryDto Users { get; set; } = new();
        public List<RoleDistributionDto> RoleDistribution { get; set; } = new();
        public PlatformOverviewDto Platform { get; set; } = new();
        public ActivitySummaryDto Activity { get; set; } = new();
        public SystemHealthDto Health { get; set; } = new();
    }
}
