// ===========================================================================================================
// File: AccountStatus.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Domain entity model representing AccountStatus in the database.
// ===========================================================================================================
namespace SmartMicrogrid.API.Models.Common
{
    /// <summary>
    /// M4 account lifecycle state. UserService keeps User.IsActive in sync so that
    /// only Active maps to IsActive = true; Inactive, Suspended and Pending all map
    /// to false and therefore block login through the existing AuthService check.
    /// </summary>
    public enum AccountStatus
    {
        Active,
        Inactive,
        Suspended,
        Pending
    }
}
