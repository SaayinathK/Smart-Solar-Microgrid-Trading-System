using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);

            // Ensure unique index on Email field for Users collection
            var userEmailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<User>(userEmailIndexKeys, indexOptions);
            Users.Indexes.CreateOne(indexModel);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>(MongoCollections.Users);
    }
}
