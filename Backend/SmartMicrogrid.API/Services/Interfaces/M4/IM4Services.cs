// ===========================================================================================================
// File: IM4Services.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Defines IM4Services components for the Smart Microgrid system.
// ===========================================================================================================
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Services.Interfaces.M4
{
    public interface IDashboardService
    {
        /// <summary>
        /// Performs build async operation.
        /// </summary>
        Task<AdminDashboardDto> BuildAsync();
    }

    public interface ISystemHealthService
    {
        /// <summary>
        /// Retrieves health async details.
        /// </summary>
        Task<SystemHealthDto> GetHealthAsync();
    }

    public interface ISystemConfigurationService
    {
        /// <summary>
        /// Retrieves async details.
        /// </summary>
        Task<ConfigurationResponseDto> GetAsync();
        /// <summary>
        /// Updates the specified async record.
        /// </summary>
        Task<ConfigurationResponseDto> UpdateAsync(UpdateConfigurationDto dto, string updatedBy);
        /// <summary>
        /// Performs set maintenance mode async operation.
        /// </summary>
        Task<ConfigurationResponseDto> SetMaintenanceModeAsync(MaintenanceModeDto dto, string updatedBy);
        /// <summary>
        /// Performs set registration mode async operation.
        /// </summary>
        Task<ConfigurationResponseDto> SetRegistrationModeAsync(RegistrationModeDto dto, string updatedBy);
    }

    public interface IActivityService
    {
        /// <summary>
        /// Retrieves paged async details.
        /// </summary>
        Task<PagedResultDto<SystemActivityDto>> GetPagedAsync(ActivityQueryDto query);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<SystemActivityDto?> GetByIdAsync(string id);
    }

    public interface IReportService
    {
        /// <summary>
        /// Retrieves user report async details.
        /// </summary>
        Task<UserReportDto> GetUserReportAsync(DateTime? from, DateTime? to, string? role, string? status);
        /// <summary>
        /// Retrieves role report async details.
        /// </summary>
        Task<List<RoleDistributionDto>> GetRoleReportAsync();
        /// <summary>
        /// Retrieves activity report async details.
        /// </summary>
        Task<ActivityReportDto> GetActivityReportAsync(DateTime? from, DateTime? to);
        /// <summary>
        /// Retrieves platform report async details.
        /// </summary>
        Task<PlatformReportDto> GetPlatformReportAsync(DateTime? from, DateTime? to);
    }
}
