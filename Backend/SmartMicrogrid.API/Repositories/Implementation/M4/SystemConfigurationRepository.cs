// ===========================================================================================================
// File: SystemConfigurationRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Repository implementation handling MongoDB operations for SystemConfiguration.
// ===========================================================================================================
using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces.M4;

namespace SmartMicrogrid.API.Repositories.Implementation.M4
{
    public class SystemConfigurationRepository : ISystemConfigurationRepository
    {
        private readonly MongoDbContext _context;
        /// <summary>
        /// Initializes a new instance of the SystemConfigurationRepository class.
        /// </summary>

        public SystemConfigurationRepository(MongoDbContext context)
        {
            // Initialize dependencies and state
            _context = context;
        }
        /// <summary>
        /// Retrieves async details.
        /// </summary>

        public async Task<SystemConfiguration?> GetAsync()
        {
            // Execute get async operations
            return await _context.SystemConfiguration
                .Find(c => c.Id == SystemConfiguration.SingletonId)
                .FirstOrDefaultAsync();
        }
        /// <summary>
        /// Retrieves or create async details.
        /// </summary>

        public async Task<SystemConfiguration> GetOrCreateAsync()
        {
            // Execute get or create async operations
            var existing = await GetAsync();
            if (existing != null)
                return existing;

            var seeded = new SystemConfiguration();
            try
            {
                await _context.SystemConfiguration.InsertOneAsync(seeded);
                return seeded;
            }
            catch (MongoWriteException)
            {
                // Another request seeded the singleton between the read and the write.
                return await GetAsync() ?? seeded;
            }
        }
        /// <summary>
        /// Performs upsert async operation.
        /// </summary>

        public async Task<SystemConfiguration> UpsertAsync(SystemConfiguration configuration)
        {
            // Execute upsert async operations
            configuration.Id = SystemConfiguration.SingletonId;
            configuration.UpdatedAt = DateTime.UtcNow;

            await _context.SystemConfiguration.ReplaceOneAsync(
                c => c.Id == SystemConfiguration.SingletonId,
                configuration,
                new ReplaceOptions { IsUpsert = true });

            return configuration;
        }
    }
}
