using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Services.Interfaces.M4
{
    public interface IDashboardService
    {
        Task<AdminDashboardDto> BuildAsync();
    }

    public interface ISystemHealthService
    {
        Task<SystemHealthDto> GetHealthAsync();
    }

    public interface ISystemConfigurationService
    {
        Task<ConfigurationResponseDto> GetAsync();
        Task<ConfigurationResponseDto> UpdateAsync(UpdateConfigurationDto dto, string updatedBy);
        Task<ConfigurationResponseDto> SetMaintenanceModeAsync(MaintenanceModeDto dto, string updatedBy);
        Task<ConfigurationResponseDto> SetRegistrationModeAsync(RegistrationModeDto dto, string updatedBy);
    }

    public interface IActivityService
    {
        Task<PagedResultDto<SystemActivityDto>> GetPagedAsync(ActivityQueryDto query);
        Task<SystemActivityDto?> GetByIdAsync(string id);
    }

    public interface IReportService
    {
        Task<UserReportDto> GetUserReportAsync(DateTime? from, DateTime? to, string? role, string? status);
        Task<List<RoleDistributionDto>> GetRoleReportAsync();
        Task<ActivityReportDto> GetActivityReportAsync(DateTime? from, DateTime? to);
        Task<PlatformReportDto> GetPlatformReportAsync(DateTime? from, DateTime? to);
    }
}
