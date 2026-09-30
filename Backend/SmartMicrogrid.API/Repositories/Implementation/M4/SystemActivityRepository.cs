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

        public SystemActivityRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<SystemActivity> RecordAsync(SystemActivity activity)
        {
            activity.Timestamp = DateTime.UtcNow;
            await _context.SystemActivity.InsertOneAsync(activity);
            return activity;
        }

        public async Task<SystemActivity?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return null;

            return await _context.SystemActivity.Find(a => a.Id == id).FirstOrDefaultAsync();
        }

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

        public async Task<List<SystemActivity>> GetRecentAsync(int count)
        {
            return await _context.SystemActivity
                .Find(Builders<SystemActivity>.Filter.Empty)
                .SortByDescending(a => a.Timestamp)
                .Limit(count)
                .ToListAsync();
        }

        public async Task<long> CountAsync(
            string? userId,
            string? module,
            string? action,
            string? status,
            DateTime? from,
            DateTime? to)
        {
            return await _context.SystemActivity
                .CountDocumentsAsync(BuildFilter(userId, module, action, status, from, to));
        }

        public async Task<Dictionary<string, long>> GetActionCountsAsync(DateTime? from, DateTime? to)
        {
            var grouped = await _context.SystemActivity
                .Aggregate()
                .Match(BuildDateRange(from, to))
                .Group(a => a.Action, g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped.ToDictionary(x => x.Key, x => (long)x.Count);
        }

        public async Task<Dictionary<string, long>> GetModuleCountsAsync(DateTime? from, DateTime? to)
        {
            var grouped = await _context.SystemActivity
                .Aggregate()
                .Match(BuildDateRange(from, to))
                .Group(a => a.Module, g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync();

            return grouped.ToDictionary(x => x.Key, x => (long)x.Count);
        }

        private static FilterDefinition<SystemActivity> BuildDateRange(DateTime? from, DateTime? to)
        {
            var builder = Builders<SystemActivity>.Filter;
            var filter = builder.Empty;

            if (from.HasValue)
                filter &= builder.Gte(a => a.Timestamp, from.Value);
            if (to.HasValue)
                filter &= builder.Lte(a => a.Timestamp, to.Value);

            return filter;
        }

        private static FilterDefinition<SystemActivity> BuildFilter(
            string? userId,
            string? module,
            string? action,
            string? status,
            DateTime? from,
            DateTime? to)
        {
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
