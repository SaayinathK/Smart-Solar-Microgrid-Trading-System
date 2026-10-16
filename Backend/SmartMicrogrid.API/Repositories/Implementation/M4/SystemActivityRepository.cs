// ===========================================================================================================
// File: SystemActivityRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Repository implementation handling MongoDB operations for SystemActivity.
// ===========================================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces.M4;

namespace SmartMicrogrid.API.Repositories.Implementation.M4
{
    public class SystemActivityRepository : ISystemActivityRepository
    {
        private readonly MongoDbContext _context;
        /// <summary>
        /// Initializes a new instance of the SystemActivityRepository class.
        /// </summary>

        public SystemActivityRepository(MongoDbContext context)
        {
            // Initialize dependencies and state
            _context = context;
        }
        /// <summary>
        /// Performs record async operation.
        /// </summary>

        public async Task<SystemActivity> RecordAsync(SystemActivity activity)
        {
            // Execute record async operations
            activity.Timestamp = DateTime.UtcNow;
            await _context.SystemActivity.InsertOneAsync(activity);
            return activity;
        }
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<SystemActivity?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            if (!ObjectId.TryParse(id, out _))
                return null;

            return await _context.SystemActivity.Find(a => a.Id == id).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Retrieves paged async details.
        /// </summary>

        public async Task<(List<SystemActivity> Items, long TotalItems)> GetPagedAsync(
            string? userId,
            string? module,
            string? action,
            string? status,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize)
        {
            // Execute get paged async operations
            var filter = BuildFilter(userId, module, action, status, from, to);

            var totalItems = await _context.SystemActivity.CountDocumentsAsync(filter);

            var items = await _context.SystemActivity
                .Find(filter)
                .SortByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalItems);
        }
        /// <summary>
        /// Retrieves recent async details.
        /// </summary>

        public async Task<List<SystemActivity>> GetRecentAsync(int count)
        {
            // Execute get recent async operations
            return await _context.SystemActivity
                .Find(Builders<SystemActivity>.Filter.Empty)
                .SortByDescending(a => a.Timestamp)
                .Limit(count)
                .ToListAsync();
        }
        /// <summary>
        /// Performs count async operation.
        /// </summary>

        public async Task<long> CountAsync(
            string? userId,
            string? module,
            string? action,
            string? status,
            DateTime? from,
            DateTime? to)
        {
            // Execute count async operations
            return await _context.SystemActivity
                .CountDocumentsAsync(BuildFilter(userId, module, action, status, from, to));
        }
        /// <summary>
        /// Retrieves action counts async details.
        /// </summary>

        public async Task<Dictionary<string, long>> GetActionCountsAsync(DateTime? from, DateTime? to)
        {
            // Execute get action counts async operations
            var grouped = await _context.SystemActivity
                .Aggregate()
                .Match(BuildDateRange(from, to))
                .Group(a => a.Action, g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped.ToDictionary(x => x.Key, x => (long)x.Count);
        }
        /// <summary>
        /// Retrieves module counts async details.
        /// </summary>

        public async Task<Dictionary<string, long>> GetModuleCountsAsync(DateTime? from, DateTime? to)
        {
            // Execute get module counts async operations
            var grouped = await _context.SystemActivity
                .Aggregate()
                .Match(BuildDateRange(from, to))
                .Group(a => a.Module, g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped.ToDictionary(x => x.Key, x => (long)x.Count);
        }
        /// <summary>
        /// Performs build date range operation.
        /// </summary>

        private static FilterDefinition<SystemActivity> BuildDateRange(DateTime? from, DateTime? to)
        {
            // Execute build date range operations
            var builder = Builders<SystemActivity>.Filter;
            var filter = builder.Empty;

            if (from.HasValue)
                filter &= builder.Gte(a => a.Timestamp, from.Value);
            if (to.HasValue)
                filter &= builder.Lte(a => a.Timestamp, to.Value);

            return filter;
        }
        /// <summary>
        /// Performs build filter operation.
        /// </summary>

        private static FilterDefinition<SystemActivity> BuildFilter(
            string? userId,
            string? module,
            string? action,
            string? status,
            DateTime? from,
            DateTime? to)
        {
            // Execute build filter operations
            var builder = Builders<SystemActivity>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(userId))
                filter &= builder.Eq(a => a.UserId, userId.Trim());
            if (!string.IsNullOrWhiteSpace(module))
                filter &= builder.Eq(a => a.Module, module.Trim());
            if (!string.IsNullOrWhiteSpace(action))
                filter &= builder.Eq(a => a.Action, action.Trim());
            if (!string.IsNullOrWhiteSpace(status))
                filter &= builder.Eq(a => a.Status, status.Trim());

            return filter & BuildDateRange(from, to);
        }
    }
}
