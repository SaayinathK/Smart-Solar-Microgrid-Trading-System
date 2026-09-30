using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Models.Transactions;
using SmartMicrogrid.API.Models.M2;
using SmartMicrogrid.API.Models.M4;

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

                Users.Indexes.CreateOne(new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Nic),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Sparse = true,
                        Name = "ux_users_nic_sparse"
                    }));

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
                    Builders<EnergySlot>.IndexKeys
                        .Ascending(s => s.StartTime)
                        .Ascending(s => s.EndTime)));

                // Indexes for Transactions collection
                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.ProsumerId)));

                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.QrCodeData)));

                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Ascending(t => t.VerifiedBy)));

                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(
                    Builders<Transaction>.IndexKeys.Descending(t => t.CreatedAt)));

                // Indexes for Reservations collection
                Reservations.Indexes.CreateOne(new CreateIndexModel<Reservation>(
                    Builders<Reservation>.IndexKeys
                        .Ascending(r => r.ProsumerId)
                        .Ascending(r => r.Status)));

                Reservations.Indexes.CreateOne(new CreateIndexModel<Reservation>(
                    Builders<Reservation>.IndexKeys
                        .Ascending(r => r.EnergySlotId)
                        .Ascending(r => r.Status)));

                // M4 - audit trail and account status filters
                SystemActivity.Indexes.CreateOne(new CreateIndexModel<SystemActivity>(
                    Builders<SystemActivity>.IndexKeys.Descending(a => a.Timestamp)));

                SystemActivity.Indexes.CreateOne(new CreateIndexModel<SystemActivity>(
                    Builders<SystemActivity>.IndexKeys.Ascending(a => a.UserId)));

                SystemActivity.Indexes.CreateOne(new CreateIndexModel<SystemActivity>(
                    Builders<SystemActivity>.IndexKeys
                        .Ascending(a => a.Module)
                        .Ascending(a => a.Timestamp)));

                Users.Indexes.CreateOne(new CreateIndexModel<User>(
                    Builders<User>.IndexKeys
                        .Ascending(u => u.AccountStatus)
                        .Ascending(u => u.CreatedAt)));
            }
            catch (InvalidOperationException)
            {
                // Preserve validation errors such as duplicate transaction ReservationIds
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
                var reservationIds = string.Join(
                    ", ",
                    duplicates.Select(x => x.ReservationId));

                throw new InvalidOperationException(
                    $"Cannot create the unique transaction ReservationId index. Duplicate ReservationIds: {reservationIds}");
            }

            var indexKeys =
                Builders<Transaction>.IndexKeys.Ascending(t => t.ReservationId);

            const string indexName = "ReservationId_1";

            var existing = Transactions.Indexes
                .List()
                .ToList()
                .FirstOrDefault(
                    index => index.GetValue("name", "").AsString == indexName);

            if (existing != null &&
                !existing.GetValue("unique", false).ToBoolean())
            {
                Transactions.Indexes.DropOne(indexName);
            }

            Transactions.Indexes.CreateOne(
                new CreateIndexModel<Transaction>(
                    indexKeys,
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = indexName
                    }));
        }

        public IMongoCollection<User> Users =>
            _database.GetCollection<User>(MongoCollections.Users);

        public IMongoCollection<BsonDocument> GetRawUsersCollection() =>
            _database.GetCollection<BsonDocument>(MongoCollections.Users);

        public IMongoCollection<MicrogridNode> Microgrids =>
            _database.GetCollection<MicrogridNode>(MongoCollections.Microgrids);

        public IMongoCollection<EnergySlot> EnergySlots =>
            _database.GetCollection<EnergySlot>(MongoCollections.EnergySlots);

        public IMongoCollection<Transaction> Transactions =>
            _database.GetCollection<Transaction>(MongoCollections.Transactions);

        public IMongoCollection<Reservation> Reservations =>
            _database.GetCollection<Reservation>(MongoCollections.Reservations);

        public IMongoCollection<SystemActivity> SystemActivity =>
            _database.GetCollection<SystemActivity>(MongoCollections.SystemActivity);

        public IMongoCollection<SystemConfiguration> SystemConfiguration =>
            _database.GetCollection<SystemConfiguration>(MongoCollections.SystemConfiguration);

        /// <summary>
        /// Exposed for M4 system health, which must actually ping MongoDB rather
        /// than assume connectivity.
        /// </summary>
        public IMongoDatabase Database => _database;
    }
}