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

        public EnergySlotRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EnergySlot>> GetAllAsync(string? microgridId = null, string? status = null, DateTime? startTime = null, DateTime? endTime = null, double? minEnergy = null)
        {
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

        public async Task<EnergySlot?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _context.EnergySlots.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<EnergySlot> CreateAsync(EnergySlot slot)
        {
            slot.CreatedAt = DateTime.UtcNow;
            slot.UpdatedAt = DateTime.UtcNow;
            await _context.EnergySlots.InsertOneAsync(slot);
            return slot;
        }

        public async Task<bool> UpdateAsync(string id, EnergySlot slot)
        {
            if (!ObjectId.TryParse(id, out _)) return false;

            slot.UpdatedAt = DateTime.UtcNow;
            var result = await _context.EnergySlots.ReplaceOneAsync(s => s.Id == id, slot);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return false;
            var result = await _context.EnergySlots.DeleteOneAsync(s => s.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateStatusAsync(string id, string status)
        {
            if (!ObjectId.TryParse(id, out _)) return false;

            var update = Builders<EnergySlot>.Update
                .Set(s => s.Status, status)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            var result = await _context.EnergySlots.UpdateOneAsync(s => s.Id == id, update);
            return result.ModifiedCount > 0;
        }

        public async Task<long> GetCountAsync(string? status = null)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return await _context.EnergySlots.CountDocumentsAsync(Builders<EnergySlot>.Filter.Empty);
            }
            return await _context.EnergySlots.CountDocumentsAsync(s => s.Status == status);
        }

        public async Task<double> GetTotalAvailableEnergyAsync()
        {
            var slots = await _context.EnergySlots.Find(s => s.Status == "Available" && s.EndTime > DateTime.UtcNow).ToListAsync();
            return slots.Sum(s => s.AvailableAmount);
        }
    }
}
