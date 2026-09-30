using MongoDB.Bson;
using MongoDB.Driver;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Repositories.Implementation
{
    public class UserRepository : IUserRepository
    {
        private readonly MongoDbContext _context;

        public UserRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null, AccountStatus? accountStatus = null)
        {
            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var regex = new BsonRegularExpression(searchTerm.Trim(), "i");
                var nameOrEmailFilter = filterBuilder.Or(
                    filterBuilder.Regex(u => u.FirstName, regex),
                    filterBuilder.Regex(u => u.LastName, regex),
                    filterBuilder.Regex(u => u.Email, regex),
                    filterBuilder.Regex(u => u.Nic, regex)
                );
                filter &= nameOrEmailFilter;
            }

            if (roleFilter.HasValue)
            {
                filter &= filterBuilder.Eq(u => u.Role, roleFilter.Value);
            }

            if (activeOnly.HasValue)
            {
                filter &= filterBuilder.Eq(u => u.IsActive, activeOnly.Value);
            }

            if (accountStatus.HasValue)
            {
                // Documents written before AccountStatus existed have no stored
                // enum, so they are classified the same way DashboardService does
                // and matched on IsActive instead.
                if (accountStatus.Value == AccountStatus.Active)
                {
                    filter &= filterBuilder.Eq(u => u.IsActive, true);
                }
                else if (accountStatus.Value == AccountStatus.Inactive)
                {
                    filter &= filterBuilder.And(
                        filterBuilder.Eq(u => u.IsActive, false),
                        filterBuilder.Ne(u => u.AccountStatus, AccountStatus.Suspended),
                        filterBuilder.Ne(u => u.AccountStatus, AccountStatus.Pending)
                    );
                }
                else
                {
                    filter &= filterBuilder.Eq(u => u.AccountStatus, accountStatus.Value);
                }
            }

            return await _context.Users.Find(filter).SortByDescending(u => u.CreatedAt).ToListAsync();
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return null;

            return await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Users.Find(u => u.Email.ToLower() == email.Trim().ToLower()).FirstOrDefaultAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.Users.InsertOneAsync(user);
            return user;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return false;

            var result = await _context.Users.DeleteOneAsync(u => u.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        public async Task<bool> ExistsByEmailAsync(string email, string? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalizedEmail = email.Trim().ToLower();
            var filter = Builders<User>.Filter.Eq(u => u.Email, normalizedEmail);

            if (!string.IsNullOrEmpty(excludeUserId) && ObjectId.TryParse(excludeUserId, out _))
            {
                filter &= Builders<User>.Filter.Ne(u => u.Id, excludeUserId);
            }

            return await _context.Users.Find(filter).AnyAsync();
        }

        public async Task<bool> ExistsByNicAsync(string nic, string? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(nic))
                return false;

            var normalizedNic = nic.Trim().ToUpper();
            var filter = Builders<User>.Filter.Eq(u => u.Nic, normalizedNic);

            if (!string.IsNullOrEmpty(excludeUserId) && ObjectId.TryParse(excludeUserId, out _))
            {
                filter &= Builders<User>.Filter.Ne(u => u.Id, excludeUserId);
            }

            return await _context.Users.Find(filter).AnyAsync();
        }

        public async Task<User?> GetByNicAsync(string nic)
        {
            if (string.IsNullOrWhiteSpace(nic))
                return null;

            var normalized = nic.Trim().ToUpperInvariant();
            return await _context.Users.Find(u => u.Nic == normalized).FirstOrDefaultAsync();
        }
    }
}
