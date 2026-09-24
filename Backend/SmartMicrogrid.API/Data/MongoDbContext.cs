using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.Transactions;

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
                EnsureUniqueReservationIndex();

                // Ensure unique index on Email field for Users collection
                var userEmailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
                var indexOptions = new CreateIndexOptions { Unique = true };
                Users.Indexes.CreateOne(new CreateIndexModel<User>(userEmailIndexKeys, indexOptions));

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

                // Indexes for Transactions collection
                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.ProsumerId)));
                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.QrCodeData)));
                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.VerifiedBy)));
                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Descending(t => t.CreatedAt)));
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                // Ignore index creation errors if MongoDB is offline during initial build setup
            }
        }

        private void EnsureUniqueReservationIndex()
        {
            var duplicates = Transactions.Aggregate()
                .Group(t => t.ReservationId, group => new
                {
                    ReservationId = group.Key,
                    Count = group.Count()
                })
                .Match(group => group.Count > 1)
                .ToList();

            if (duplicates.Count > 0)
            {
                var reservationIds = string.Join(", ", duplicates.Select(x => x.ReservationId));
                throw new InvalidOperationException(
                    $"Cannot create the unique transaction ReservationId index. Duplicate ReservationIds: {reservationIds}");
            }

            var indexKeys = Builders<Transaction>.IndexKeys.Ascending(t => t.ReservationId);
            const string indexName = "ReservationId_1";
            var existing = Transactions.Indexes.List()
                .ToList()
                .FirstOrDefault(index => index.GetValue("name", "").AsString == indexName);

            if (existing != null && !existing.GetValue("unique", false).ToBoolean())
            {
                Transactions.Indexes.DropOne(indexName);
            }

            Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                indexKeys,
                new CreateIndexOptions { Unique = true, Name = indexName }));
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>(MongoCollections.Users);
        public IMongoCollection<MicrogridNode> Microgrids => _database.GetCollection<MicrogridNode>(MongoCollections.Microgrids);
        public IMongoCollection<EnergySlot> EnergySlots => _database.GetCollection<EnergySlot>(MongoCollections.EnergySlots);
        public IMongoCollection<Transaction> Transactions => _database.GetCollection<Transaction>(MongoCollections.Transactions);
    }
}
