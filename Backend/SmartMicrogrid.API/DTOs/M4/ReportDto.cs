using System;
using System.ComponentModel.DataAnnotations;

namespace SmartMicrogrid.API.DTOs.M4
{
    public class ActivityQueryDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
        public int Page { get; set; } = 1;

        [Range(1, 200, ErrorMessage = "Page size must be between 1 and 200.")]
        public int PageSize { get; set; } = 20;

        public string? UserId { get; set; }
        public string? Module { get; set; }
        public string? Action { get; set; }
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class UserSummaryRowDto
    {
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long Count { get; set; }
    }

    public class UserReportDto
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public long TotalUsers { get; set; }
        public long ActiveUsers { get; set; }
        public long InactiveUsers { get; set; }
        public long SuspendedUsers { get; set; }
        public long PendingUsers { get; set; }
        public long NewUsersInPeriod { get; set; }

        public List<UserSummaryRowDto> ByStatus { get; set; } = new();
        public List<RoleDistributionDto> ByRole { get; set; } = new();
    }

    public class PlatformReportDto
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public DashboardSummaryDto Users { get; set; } = new();
        public List<RoleDistributionDto> RoleDistribution { get; set; } = new();
        public PlatformOverviewDto Platform { get; set; } = new();
        public ActivityReportDto Activity { get; set; } = new();
    }
}
