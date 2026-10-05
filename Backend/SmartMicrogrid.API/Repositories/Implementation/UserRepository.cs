// ===========================================================================================================
// File: UserRepository.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Repository implementation handling MongoDB operations for User.
// ===========================================================================================================
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
        /// <summary>
        /// Initializes a new instance of the UserRepository class.
        /// </summary>

        public UserRepository(MongoDbContext context)
        {
            // Initialize dependencies and state
            _context = context;
        }
        /// <summary>
        /// Retrieves all async details.
        /// </summary>

        public async Task<IEnumerable<User>> GetAllAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null, AccountStatus? accountStatus = null)
        {
            // Execute get all async operations
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
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>

        public async Task<User?> GetByIdAsync(string id)
        {
            // Execute get by id async operations
            if (!ObjectId.TryParse(id, out _))
                return null;

            return await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Retrieves by email async details.
        /// </summary>

        public async Task<User?> GetByEmailAsync(string email)
        {
            // Execute get by email async operations
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Users.Find(u => u.Email.ToLower() == email.Trim().ToLower()).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>

        public async Task<User> CreateAsync(User user)
        {
            // Execute create async operations
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.Users.InsertOneAsync(user);
            return user;
        }
        /// <summary>
        /// Updates the specified async record.
        /// </summary>

        public async Task<bool> UpdateAsync(User user)
        {
            // Execute update async operations
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>

        public async Task<bool> DeleteAsync(string id)
        {
            // Execute delete async operations
            if (!ObjectId.TryParse(id, out _))
                return false;

            var result = await _context.Users.DeleteOneAsync(u => u.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
        /// <summary>
        /// Performs exists by email async operation.
        /// </summary>

        public async Task<bool> ExistsByEmailAsync(string email, string? excludeUserId = null)
        {
            // Execute exists by email async operations
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
        /// <summary>
        /// Performs exists by nic async operation.
        /// </summary>

        public async Task<bool> ExistsByNicAsync(string nic, string? excludeUserId = null)
        {
            // Execute exists by nic async operations
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
        /// <summary>
        /// Retrieves by nic async details.
        /// </summary>

        public async Task<User?> GetByNicAsync(string nic)
        {
            // Execute get by nic async operations
            if (string.IsNullOrWhiteSpace(nic))
                return null;

            var normalized = nic.Trim().ToUpperInvariant();
            return await _context.Users.Find(u => u.Nic == normalized).FirstOrDefaultAsync();
        }
    }
}
