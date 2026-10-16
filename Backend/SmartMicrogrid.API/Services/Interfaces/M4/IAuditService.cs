// ===========================================================================================================
// File: IAuditService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Service interface defining contract for Audit operations.
// ===========================================================================================================
using SmartMicrogrid.API.Models.M4;

namespace SmartMicrogrid.API.Services.Interfaces.M4
{
    /// <summary>
    /// Shared audit entry point. M1/M2/M3 can call this from their own services to
    /// contribute to the platform activity trail without M4 taking ownership of
    /// their business logic.
    /// </summary>
    public interface IAuditService
    {
        /// <remarks>
        /// Every parameter after <paramref name="description"/> is optional and is
        /// resolved from the current <see cref="System.Security.Claims.ClaimsPrincipal"/>
        /// when omitted. Do not add a three-argument overload: it would bind in
        /// preference to this one and recurse.
        /// </remarks>
        Task RecordAsync(
            string action,
            string module,
            string description,
            string? userId = null,
            string? userName = null,
            string? role = null,
            string? entityType = null,
            string? entityId = null,
            string? ipAddress = null,
            string status = AuditStatus.Success);
    }
}
