using SmartMicrogrid.API.Models.M4;

namespace SmartMicrogrid.API.Repositories.Interfaces.M4
{
    public interface ISystemConfigurationRepository
    {
        Task<SystemConfiguration?> GetAsync();
        Task<SystemConfiguration> GetOrCreateAsync();
        Task<SystemConfiguration> UpsertAsync(SystemConfiguration configuration);
    }
}
