using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces.M4;

namespace SmartMicrogrid.API.Repositories.Implementation.M4
{
    public class SystemConfigurationRepository : ISystemConfigurationRepository
    {
        private readonly MongoDbContext _context;

        public SystemConfigurationRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<SystemConfiguration?> GetAsync()
        {
            return await _context.SystemConfiguration
                .Find(c => c.Id == SystemConfiguration.SingletonId)
                .FirstOrDefaultAsync();
        }

        public async Task<SystemConfiguration> GetOrCreateAsync()
        {
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

        public async Task<SystemConfiguration> UpsertAsync(SystemConfiguration configuration)
        {
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
