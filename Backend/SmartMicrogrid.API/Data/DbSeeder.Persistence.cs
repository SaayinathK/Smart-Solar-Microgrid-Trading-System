using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SmartMicrogrid.API.Data;

public static partial class DbSeeder
{
    // Stable IDs make repeated runs insert-only, including after a partially completed run.
    private static string SeedId(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"smartmicrogrid-demo-v2:{key}")))[..24]
            .ToLowerInvariant();

    /// <summary>
    /// Inserts missing demonstration records without resetting collections or updating existing records.
    /// Existing users are matched by email/NIC; existing nodes by name and operator ownership.
    /// </summary>
    public static async Task SeedAllAsync(MongoDbContext context)
    {
        var data = CreateDemoData(DateTime.UtcNow);
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var existingUsers = await context.Database.GetCollection<BsonDocument>(MongoCollections.Users)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        foreach (var user in data[MongoCollections.Users])
        {
            var matches = existingUsers.Where(existing =>
                string.Equals(existing.GetValue("email", "").AsString, user["email"].AsString,
                    StringComparison.OrdinalIgnoreCase) ||
                existing.GetValue("nic", BsonNull.Value) == user["nic"]).ToList();
            if (matches.Count > 1)
                throw new InvalidOperationException("Demo user email and NIC match different existing accounts. No seed records were inserted.");
            if (matches.Count == 1)
                ids[user["_id"].ToString()!] = matches[0]["_id"].ToString()!;
        }

        var existingNodes = await context.Database.GetCollection<BsonDocument>(MongoCollections.Microgrids)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        foreach (var node in data[MongoCollections.Microgrids])
        {
            var owner = node["operatorId"].ToString()!;
            owner = ids.GetValueOrDefault(owner, owner);
            var matches = existingNodes.Where(existing => existing.GetValue("name", "") == node["name"] &&
                existing.GetValue("operatorId", "").ToString() == owner).ToList();
            if (matches.Count > 1)
                throw new InvalidOperationException("Multiple existing nodes match a demo node and its operator. No seed records were inserted.");
            if (matches.Count == 1)
                ids[node["_id"].ToString()!] = matches[0]["_id"].ToString()!;
        }

        foreach (var documents in data.Values)
            foreach (var document in documents)
                RemapReferences(document, ids);

        Console.WriteLine($"Seeding database '{context.Database.DatabaseNamespace.DatabaseName}' (insert missing records only; prices in LKR).");
        long inserted = 0;
        foreach (var (collectionName, documents) in data)
        {
            var collection = context.Database.GetCollection<BsonDocument>(collectionName);
            var writes = documents.Select(document =>
            {
                var values = document.DeepClone().AsBsonDocument;
                values.Remove("_id");
                return new UpdateOneModel<BsonDocument>(
                    new BsonDocument("_id", document["_id"]),
                    new BsonDocument("$setOnInsert", values)) { IsUpsert = true };
            }).ToList();
            var result = await collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = true });
            inserted += result.Upserts.Count;

            // Read back every seeded ID, including reused users and nodes, before reporting success.
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.In("_id", documents.Select(d => d["_id"])));
            if (count != documents.Count)
                throw new InvalidOperationException($"Seed verification failed for {collectionName}.");
            Console.WriteLine($"  {collectionName}: {result.Upserts.Count} inserted, {result.MatchedCount} preserved, {count} demo records verified.");
        }
        Console.WriteLine($"Seed complete: {inserted} records inserted across {data.Count} collections. Existing records preserved.");
    }

    private static void RemapReferences(BsonDocument document, IReadOnlyDictionary<string, string> ids)
    {
        foreach (var name in document.Names.ToList())
        {
            var value = document[name];
            if (value is BsonDocument nested) RemapReferences(nested, ids);
            else if (value is BsonArray array)
            {
                foreach (var item in array.OfType<BsonDocument>()) RemapReferences(item, ids);
            }
            else if ((value.IsObjectId || value.IsString) && ids.TryGetValue(value.ToString()!, out var mapped))
                document[name] = value.IsObjectId ? new BsonObjectId(ObjectId.Parse(mapped)) : new BsonString(mapped);
        }
    }
}
