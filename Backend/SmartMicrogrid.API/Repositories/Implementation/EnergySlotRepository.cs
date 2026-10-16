// ===========================================================================================================
// File: EnergySlotRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Repository implementation handling MongoDB operations for EnergySlot.
// ===========================================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Repositories.Implementation
{
    public class EnergySlotRepository : IEnergySlotRepository
    {
        private readonly MongoDbContext _context;
        /// <summary>
        /// Initializes a new instance of the EnergySlotRepository class.
        /// </summary>

        public EnergySlotRepository(MongoDbContext context)
        {
            // Initialize dependencies and state
            _context = context;
        }
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        public async Task<IEnumerable<EnergySlot>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null)
        {
            // Execute get all async operations
            var builder = Builders<EnergySlot>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(microgridId) && ObjectId.TryParse(microgridId, out _))
            {
                filter &= builder.Eq(s => s.MicrogridNodeId, microgridId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filter &= builder.Eq(s => s.Status, status);
            }

            if (startTime.HasValue)
            {
                filter &= builder.Gte(s => s.StartTime, startTime.Value);
            }

            if (endTime.HasValue)
            {
                filter &= builder.Lte(s => s.EndTime, endTime.Value);
            }

            if (minEnergy.HasValue)
            {
                filter &= builder.Gte(s => s.AvailableAmount, minEnergy.Value);
            }

            return await _context.EnergySlots.Find(filter).SortBy(s => s.StartTime).ToListAsync();
        }
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<EnergySlot?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _context.EnergySlots.Find(s => s.Id == id).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>

        public async Task<EnergySlot> CreateAsync(EnergySlot slot)
        {
            // Execute create async operations
            slot.CreatedAt = DateTime.UtcNow;
            slot.UpdatedAt = DateTime.UtcNow;
            await _context.EnergySlots.InsertOneAsync(slot);
            return slot;
        }
        /// <summary>
        /// Updates the specified async record.
        /// </summary>

        public async Task<bool> UpdateAsync(string id, EnergySlot slot)
        {
            // Execute update async operations
            if (!ObjectId.TryParse(id, out _)) return false;

            slot.UpdatedAt = DateTime.UtcNow;
            var result = await _context.EnergySlots.ReplaceOneAsync(s => s.Id == id, slot);
            return result.ModifiedCount > 0;
        }
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>

        public async Task<bool> DeleteAsync(string id)
        {
            // Execute delete async operations
            if (!ObjectId.TryParse(id, out _)) return false;
            var result = await _context.EnergySlots.DeleteOneAsync(s => s.Id == id);
            return result.DeletedCount > 0;
        }
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>

        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
            // Execute update status async operations
            if (!ObjectId.TryParse(id, out _)) return false;

            var update = Builders<EnergySlot>.Update
                .Set(s => s.Status, status)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            var result = await _context.EnergySlots.UpdateOneAsync(s => s.Id == id, update);
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
                return await _context.EnergySlots.CountDocumentsAsync(Builders<EnergySlot>.Filter.Empty);
            }
            return await _context.EnergySlots.CountDocumentsAsync(s => s.Status == status);
        }
        /// <summary>
        /// Retrieves total available energy async details.
        /// </summary>

        public async Task<double> GetTotalAvailableEnergyAsync()
        {
            // Execute get total available energy async operations
            var slots = await _context.EnergySlots.Find(s => s.Status == "Available" && s.EndTime > DateTime.UtcNow).ToListAsync();
            return slots.Sum(s => s.AvailableAmount);
        }
    }
}
