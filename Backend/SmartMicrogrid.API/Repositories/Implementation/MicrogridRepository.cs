using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Repositories.Implementation
{
    public class MicrogridRepository : IMicrogridRepository
    {
        private readonly MongoDbContext _context;

        public MicrogridRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MicrogridNode>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null)
        {
            var builder = Builders<MicrogridNode>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(status))
            {
                filter &= builder.Eq(m => m.Status, status);
            }

            if (isActive.HasValue)
            {
                filter &= builder.Eq(m => m.IsActive, isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                filter &= builder.Regex(m => m.Location, new BsonRegularExpression(location, "i"));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchFilter = builder.Regex(m => m.Name, new BsonRegularExpression(search, "i")) |
                                   builder.Regex(m => m.Location, new BsonRegularExpression(search, "i")) |
                                   builder.Regex(m => m.Description, new BsonRegularExpression(search, "i"));
                filter &= searchFilter;
            }

            return await _context.Microgrids.Find(filter).SortByDescending(m => m.CreatedAt).ToListAsync();
        }

        public async Task<MicrogridNode?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _context.Microgrids.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<MicrogridNode> CreateAsync(MicrogridNode microgrid)
        {
            microgrid.CreatedAt = DateTime.UtcNow;
            microgrid.UpdatedAt = DateTime.UtcNow;
            await _context.Microgrids.InsertOneAsync(microgrid);
            return microgrid;
        }

        public async Task<bool> UpdateAsync(string id, MicrogridNode microgrid)
        {
            if (!ObjectId.TryParse(id, out _)) return false;

            microgrid.UpdatedAt = DateTime.UtcNow;
            var result = await _context.Microgrids.ReplaceOneAsync(m => m.Id == id, microgrid);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return false;
            var result = await _context.Microgrids.DeleteOneAsync(m => m.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateStatusAsync(string id, string status, bool isActive)
        {
            if (!ObjectId.TryParse(id, out _)) return false;

            var update = Builders<MicrogridNode>.Update
                .Set(m => m.Status, status)
                .Set(m => m.IsActive, isActive)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Microgrids.UpdateOneAsync(m => m.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateCapacityAsync(string id, double totalCapacity, double availableCapacity, double reservedCapacity, double usedCapacity)
        {
            if (!ObjectId.TryParse(id, out _)) return false;

            var update = Builders<MicrogridNode>.Update
                .Set(m => m.Capacity, totalCapacity)
                .Set(m => m.AvailableCapacity, availableCapacity)
                .Set(m => m.ReservedCapacity, reservedCapacity)
                .Set(m => m.UsedCapacity, usedCapacity)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Microgrids.UpdateOneAsync(m => m.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBatteryAsync(string id, double batteryCapacity, double currentBatteryLevel, double batteryPercentage, string batteryStatus)
        {
            if (!ObjectId.TryParse(id, out _)) return false;

            var update = Builders<MicrogridNode>.Update
                .Set(m => m.BatteryCapacity, batteryCapacity)
                .Set(m => m.CurrentBatteryLevel, currentBatteryLevel)
                .Set(m => m.BatteryPercentage, batteryPercentage)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Microgrids.UpdateOneAsync(m => m.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<long> GetCountAsync(string? status = null)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return await _context.Microgrids.CountDocumentsAsync(Builders<MicrogridNode>.Filter.Empty);
            }
            return await _context.Microgrids.CountDocumentsAsync(m => m.Status == status);
        }
    }
}
