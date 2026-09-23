using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IMicrogridDashboardService
    {
        Task<MicrogridDashboardDto> GetDashboardStatsAsync();
    }
}
