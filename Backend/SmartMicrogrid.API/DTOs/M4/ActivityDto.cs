using System;
using System.Collections.Generic;

namespace SmartMicrogrid.API.DTOs.M4
{
    public class SystemActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shared pagination envelope for M4 list endpoints.
    /// </summary>
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class ActivityReportDto
    {
        public long TotalEvents { get; set; }
        public long SuccessEvents { get; set; }
        public long FailureEvents { get; set; }
        public Dictionary<string, long> ByAction { get; set; } = new();
        public Dictionary<string, long> ByModule { get; set; } = new();
        public List<SystemActivityDto> Recent { get; set; } = new();
    }
}
