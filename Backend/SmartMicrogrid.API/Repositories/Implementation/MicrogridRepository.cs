// ===========================================================================================================
// File: MicrogridRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Repository implementation handling MongoDB operations for Microgrid.
// ===========================================================================================================
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
        /// <summary>
        /// Initializes a new instance of the MicrogridRepository class.
        /// </summary>

        public MicrogridRepository(MongoDbContext context)
        {
            // Initialize dependencies and state
            _context = context;
        }
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        public async Task<IEnumerable<MicrogridNode>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null)
        {
            // Execute get all async operations
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
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<MicrogridNode?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _context.Microgrids.Find(m => m.Id == id).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>

        public async Task<MicrogridNode> CreateAsync(MicrogridNode microgrid)
        {
            // Execute create async operations
            microgrid.CreatedAt = DateTime.UtcNow;
            microgrid.UpdatedAt = DateTime.UtcNow;
            await _context.Microgrids.InsertOneAsync(microgrid);
            return microgrid;
        }
        /// <summary>
        /// Updates the specified async record.
        /// </summary>

        public async Task<bool> UpdateAsync(string id, MicrogridNode microgrid)
        {
            // Execute update async operations
            if (!ObjectId.TryParse(id, out _)) return false;

            microgrid.UpdatedAt = DateTime.UtcNow;
            var result = await _context.Microgrids.ReplaceOneAsync(m => m.Id == id, microgrid);
            return result.ModifiedCount > 0;
        }
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>

        public async Task<bool> DeleteAsync(string id)
        {
            // Execute delete async operations
            if (!ObjectId.TryParse(id, out _)) return false;
            var result = await _context.Microgrids.DeleteOneAsync(m => m.Id == id);
            return result.DeletedCount > 0;
        }
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>

        public async Task<bool> UpdateStatusAsync(string id, string status, bool isActive)
        {
            // Execute update status async operations
            if (!ObjectId.TryParse(id, out _)) return false;

            var update = Builders<MicrogridNode>.Update
                .Set(m => m.Status, status)
                .Set(m => m.IsActive, isActive)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Microgrids.UpdateOneAsync(m => m.Id == id, update);
            return result.ModifiedCount > 0;
        }
        /// <summary>
        /// Updates the specified capacity async record.
        /// </summary>

        public async Task<bool> UpdateCapacityAsync(string id, double totalCapacity, double availableCapacity, double reservedCapacity, double usedCapacity)
        {
            // Execute update capacity async operations
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
        /// <summary>
        /// Updates the specified battery async record.
        /// </summary>

        public async Task<bool> UpdateBatteryAsync(string id, double batteryCapacity, double currentBatteryLevel, double batteryPercentage, string batteryStatus)
        {
            // Execute update battery async operations
            if (!ObjectId.TryParse(id, out _)) return false;

            var update = Builders<MicrogridNode>.Update
                .Set(m => m.BatteryCapacity, batteryCapacity)
                .Set(m => m.CurrentBatteryLevel, currentBatteryLevel)
                .Set(m => m.BatteryPercentage, batteryPercentage)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _context.Microgrids.UpdateOneAsync(m => m.Id == id, update);
            return result.ModifiedCount > 0;
        }
        /// <summary>
        /// Retrieves count async details.
        /// </summary>

        public async Task<long> GetCountAsync(string? status = null)
        {
            // Execute get count async operations
            if (string.IsNullOrWhiteSpace(status))
            {
                return await _context.Microgrids.CountDocumentsAsync(Builders<MicrogridNode>.Filter.Empty);
            }
            return await _context.Microgrids.CountDocumentsAsync(m => m.Status == status);
        }
    }
}
