using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IMicrogridService
    {
        Task<IEnumerable<MicrogridResponseDto>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null);
        Task<MicrogridResponseDto?> GetByIdAsync(string id);
        Task<MicrogridResponseDto> CreateAsync(CreateMicrogridDto dto, string operatorId);
        Task<MicrogridResponseDto?> UpdateAsync(string id, UpdateMicrogridDto dto);
        Task<bool> DeleteAsync(string id);
        Task<bool> UpdateStatusAsync(string id, string status);
    }
}
