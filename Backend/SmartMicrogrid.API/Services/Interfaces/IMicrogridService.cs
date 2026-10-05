// ===========================================================================================================
// File: IMicrogridService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Service interface defining contract for Microgrid operations.
// ===========================================================================================================
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;

namespace SmartMicrogrid.API.Services.Interfaces
{
    public interface IMicrogridService
    {
        /// <summary>
        /// Retrieves all async details.
        /// </summary>
        Task<IEnumerable<MicrogridResponseDto>> GetAllAsync(string? status = null, bool? isActive = null, string? location = null, string? search = null);
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>
        Task<MicrogridResponseDto?> GetByIdAsync(string id);
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>
        Task<MicrogridResponseDto> CreateAsync(CreateMicrogridDto dto, string operatorId);
        /// <summary>
        /// Updates the specified async record.
        /// </summary>
        Task<MicrogridResponseDto?> UpdateAsync(string id, UpdateMicrogridDto dto);
        /// <summary>
        /// Deletes or removes the designated async record.
        /// </summary>
        Task<bool> DeleteAsync(string id);
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>
        Task<bool> UpdateStatusAsync(string id, string status);
    }
}
