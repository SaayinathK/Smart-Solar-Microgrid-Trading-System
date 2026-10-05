// ===========================================================================================================
// File: ISystemConfigurationRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Repository interface defining data access contracts for SystemConfiguration.
// ===========================================================================================================
using SmartMicrogrid.API.Models.M4;

namespace SmartMicrogrid.API.Repositories.Interfaces.M4
{
    public interface ISystemConfigurationRepository
    {
        /// <summary>
        /// Retrieves async details.
        /// </summary>
        Task<SystemConfiguration?> GetAsync();
        /// <summary>
        /// Retrieves or create async details.
        /// </summary>
        Task<SystemConfiguration> GetOrCreateAsync();
        /// <summary>
        /// Performs upsert async operation.
        /// </summary>
        Task<SystemConfiguration> UpsertAsync(SystemConfiguration configuration);
    }
}
