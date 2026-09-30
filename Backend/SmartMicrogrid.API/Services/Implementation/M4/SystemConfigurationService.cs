using System;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces.M4;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation.M4
{
    public class SystemConfigurationService : ISystemConfigurationService
    {
        private readonly ISystemConfigurationRepository _repository;
        private readonly IAuditService _auditService;

        public SystemConfigurationService(
            ISystemConfigurationRepository repository,
            IAuditService auditService)
        {
            _repository = repository;
            _auditService = auditService;
        }

        public async Task<ConfigurationResponseDto> GetAsync()
        {
            var configuration = await _repository.GetOrCreateAsync();
            return ToDto(configuration);
        }

        public async Task<ConfigurationResponseDto> UpdateAsync(UpdateConfigurationDto dto, string updatedBy)
        {
            var configuration = await _repository.GetOrCreateAsync();

            configuration.PlatformName = dto.PlatformName.Trim();
            configuration.PlatformDescription = dto.PlatformDescription?.Trim() ?? string.Empty;

            // A null message means "not supplied", not "clear it". Clients that do
            // not render the maintenance banner must not erase a live one while
            // maintenance mode is still on. An explicit empty string still clears it.
            if (dto.MaintenanceMessage is not null)
            {
                configuration.MaintenanceMessage = dto.MaintenanceMessage.Trim();
            }

            configuration.SessionTimeoutMinutes = dto.SessionTimeoutMinutes;
            configuration.MaxLoginAttempts = dto.MaxLoginAttempts;
            configuration.DefaultPageSize = dto.DefaultPageSize;
            configuration.UpdatedBy = updatedBy;

            var saved = await _repository.UpsertAsync(configuration);

            await _auditService.RecordAsync(
                AuditAction.ConfigurationUpdated,
                AuditModule.SystemConfiguration,
                $"Platform configuration updated: {saved.PlatformName}.",
                userName: updatedBy,
                entityType: nameof(SystemConfiguration),
                entityId: saved.Id);

            return ToDto(saved);
        }

        public async Task<ConfigurationResponseDto> SetMaintenanceModeAsync(MaintenanceModeDto dto, string updatedBy)
        {
            var configuration = await _repository.GetOrCreateAsync();

            configuration.MaintenanceMode = dto.MaintenanceMode;
            configuration.MaintenanceMessage = dto.MaintenanceMode
                ? dto.MaintenanceMessage?.Trim()
                : null;
            configuration.UpdatedBy = updatedBy;

            var saved = await _repository.UpsertAsync(configuration);

            await _auditService.RecordAsync(
                AuditAction.ConfigurationUpdated,
                AuditModule.SystemConfiguration,
                saved.MaintenanceMode
                    ? "Maintenance mode enabled."
                    : "Maintenance mode disabled.",
                userName: updatedBy,
                entityType: nameof(SystemConfiguration),
                entityId: saved.Id);

            return ToDto(saved);
        }

        public async Task<ConfigurationResponseDto> SetRegistrationModeAsync(RegistrationModeDto dto, string updatedBy)
        {
            var configuration = await _repository.GetOrCreateAsync();

            configuration.AllowRegistration = dto.AllowRegistration;
            configuration.UpdatedBy = updatedBy;

            var saved = await _repository.UpsertAsync(configuration);

            await _auditService.RecordAsync(
                AuditAction.ConfigurationUpdated,
                AuditModule.SystemConfiguration,
                saved.AllowRegistration
                    ? "Public registration enabled."
                    : "Public registration disabled.",
                userName: updatedBy,
                entityType: nameof(SystemConfiguration),
                entityId: saved.Id);

            return ToDto(saved);
        }

        internal static ConfigurationResponseDto ToDto(SystemConfiguration configuration) => new()
        {
            PlatformName = configuration.PlatformName,
            PlatformDescription = configuration.PlatformDescription,
            MaintenanceMode = configuration.MaintenanceMode,
            MaintenanceMessage = configuration.MaintenanceMessage,
            AllowRegistration = configuration.AllowRegistration,
            SessionTimeoutMinutes = configuration.SessionTimeoutMinutes,
            MaxLoginAttempts = configuration.MaxLoginAttempts,
            DefaultPageSize = configuration.DefaultPageSize,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt,
            UpdatedBy = configuration.UpdatedBy
        };
    }
}
