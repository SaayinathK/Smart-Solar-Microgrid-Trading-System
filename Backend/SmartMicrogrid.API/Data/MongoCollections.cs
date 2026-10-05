// ===========================================================================================================
// File: MongoCollections.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Defines MongoCollections components for the Smart Microgrid system.
// ===========================================================================================================
namespace SmartMicrogrid.API.Data
{
    public static class MongoCollections
    {
        public const string Users = "users";
        public const string Microgrids = "microgrids";
        public const string EnergySlots = "energySlots";
        public const string Reservations = "reservations";
        public const string Transactions = "transactions";

        // M4 - Platform Administration & System Operations
        public const string SystemActivity = "systemActivity";
        public const string SystemConfiguration = "systemConfiguration";
    }
}
