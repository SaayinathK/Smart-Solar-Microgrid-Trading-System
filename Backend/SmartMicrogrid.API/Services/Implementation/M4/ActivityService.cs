using System;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Repositories.Interfaces.M4;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation.M4
{
    public class ActivityService : IActivityService
    {
        private readonly ISystemActivityRepository _repository;

        public ActivityService(ISystemActivityRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResultDto<SystemActivityDto>> GetPagedAsync(ActivityQueryDto query)
        {
            if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            {
                throw new ArgumentException("The 'from' date must not be later than the 'to' date.");
            }

            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize switch
            {
                < 1 => 20,
                > 200 => 200,
                _ => query.PageSize
            };

            var (items, totalItems) = await _repository.GetPagedAsync(
                query.UserId,
                query.Module,
                query.Action,
                query.Status,
                query.From,
                query.To,
                page,
                pageSize);

            return new PagedResultDto<SystemActivityDto>
            {
                Items = items.ConvertAll(ActivityMapper.ToDto),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = pageSize == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }

        public async Task<SystemActivityDto?> GetByIdAsync(string id)
        {
            var activity = await _repository.GetByIdAsync(id);
            return activity == null ? null : ActivityMapper.ToDto(activity);
        }
    }
}
