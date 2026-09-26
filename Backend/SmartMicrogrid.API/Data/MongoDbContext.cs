using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.M2;

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
                Users.Indexes.CreateOne(new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Nic),
                    new CreateIndexOptions { Unique = true, Sparse = true, Name = "ux_users_nic_sparse" }));

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

                Reservations.Indexes.CreateOne(new CreateIndexModel<Reservation>(
                    Builders<Reservation>.IndexKeys.Ascending(r => r.ProsumerId).Ascending(r => r.Status)));
                Reservations.Indexes.CreateOne(new CreateIndexModel<Reservation>(
                    Builders<Reservation>.IndexKeys.Ascending(r => r.EnergySlotId).Ascending(r => r.Status)));
            }
            catch
            {
                // Ignore index creation errors if MongoDB is offline during initial build setup
            }
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>(MongoCollections.Users);
        public IMongoCollection<BsonDocument> GetRawUsersCollection() => _database.GetCollection<BsonDocument>(MongoCollections.Users);
        public IMongoCollection<MicrogridNode> Microgrids => _database.GetCollection<MicrogridNode>(MongoCollections.Microgrids);
        public IMongoCollection<EnergySlot> EnergySlots => _database.GetCollection<EnergySlot>(MongoCollections.EnergySlots);
        public IMongoCollection<Reservation> Reservations => _database.GetCollection<Reservation>(MongoCollections.Reservations);
    }
}
