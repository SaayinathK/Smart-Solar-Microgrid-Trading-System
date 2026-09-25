using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync(string? searchTerm = null, Role? roleFilter = null, bool? activeOnly = null);
        Task<User?> GetByIdAsync(string id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> DeleteAsync(string id);
        Task<bool> ExistsByEmailAsync(string email, string? excludeUserId = null);
        Task<bool> ExistsByNicAsync(string nic, string? excludeUserId = null);
    }
}
