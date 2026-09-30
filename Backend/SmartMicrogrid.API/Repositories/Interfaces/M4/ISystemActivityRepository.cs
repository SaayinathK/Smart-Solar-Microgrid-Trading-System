using SmartMicrogrid.API.Models.M4;

namespace SmartMicrogrid.API.Repositories.Interfaces.M4
{
    public interface ISystemActivityRepository
    {
        Task<SystemActivity> RecordAsync(SystemActivity activity);
        Task<SystemActivity?> GetByIdAsync(string id);
        Task<List<SystemActivity>> GetRecentAsync(int count);
        Task<(List<SystemActivity> Items, long TotalItems)> GetPagedAsync(
            string? userId,
            string? module,
            string? action,
            string? status,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize);
        Task<long> CountAsync(
            string? userId = null,
            string? module = null,
            string? action = null,
            string? status = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<Dictionary<string, long>> GetActionCountsAsync(DateTime? from, DateTime? to);
        Task<Dictionary<string, long>> GetModuleCountsAsync(DateTime? from, DateTime? to);
    }
}
