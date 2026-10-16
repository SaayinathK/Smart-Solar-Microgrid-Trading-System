// ===========================================================================================================
// File: RoleCompatibility.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Domain entity model representing RoleCompatibility in the database.
// ===========================================================================================================
namespace SmartMicrogrid.API.Models.Common;

public static class RoleCompatibility
{
    /// <summary>
    /// Performs to stored role operation.
    /// </summary>
    public static Role ToStoredRole(Role role) => role;
    /// <summary>
    /// Performs to assignment role operation.
    /// </summary>

    public static string ToAssignmentRole(Role role) => role.ToString();
    /// <summary>
    /// Performs to legacy role operation.
    /// </summary>

    public static string ToLegacyRole(Role role) => role.ToString();
}
