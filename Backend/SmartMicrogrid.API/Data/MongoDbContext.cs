using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;

namespace SmartMicrogrid.API.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);

            try
            {
                // Ensure unique index on Email field for Users collection
                var userEmailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
                var indexOptions = new CreateIndexOptions { Unique = true };
                Users.Indexes.CreateOne(new CreateIndexModel<User>(userEmailIndexKeys, indexOptions));

                // Index on Nic for Users collection
                var userNicIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Nic);
                Users.Indexes.CreateOne(new CreateIndexModel<User>(userNicIndexKeys));

                // Indexes for Microgrids collection
                Microgrids.Indexes.CreateOne(new CreateIndexModel<MicrogridNode>(
                    Builders<MicrogridNode>.IndexKeys.Ascending(m => m.Status)));
                Microgrids.Indexes.CreateOne(new CreateIndexModel<MicrogridNode>(
                    Builders<MicrogridNode>.IndexKeys.Ascending(m => m.IsActive)));
                Microgrids.Indexes.CreateOne(new CreateIndexModel<MicrogridNode>(
                    Builders<MicrogridNode>.IndexKeys.Ascending(m => m.OperatorId)));
                Microgrids.Indexes.CreateOne(new CreateIndexModel<MicrogridNode>(
                    Builders<MicrogridNode>.IndexKeys.Ascending(m => m.Location)));

                // Indexes for EnergySlots collection
                EnergySlots.Indexes.CreateOne(new CreateIndexModel<EnergySlot>(
                    Builders<EnergySlot>.IndexKeys.Ascending(s => s.MicrogridNodeId)));
                EnergySlots.Indexes.CreateOne(new CreateIndexModel<EnergySlot>(
                    Builders<EnergySlot>.IndexKeys.Ascending(s => s.Status)));
                EnergySlots.Indexes.CreateOne(new CreateIndexModel<EnergySlot>(
                    Builders<EnergySlot>.IndexKeys.Ascending(s => s.StartTime).Ascending(s => s.EndTime)));
            }
            catch
            {
                // Ignore index creation errors if MongoDB is offline during initial build setup
            }
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>(MongoCollections.Users);
        public IMongoCollection<MicrogridNode> Microgrids => _database.GetCollection<MicrogridNode>(MongoCollections.Microgrids);
        public IMongoCollection<EnergySlot> EnergySlots => _database.GetCollection<EnergySlot>(MongoCollections.EnergySlots);
    }
}
