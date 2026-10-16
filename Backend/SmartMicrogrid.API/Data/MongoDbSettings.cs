// ===========================================================================================================
// File: MongoDbSettings.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Defines MongoDbSettings components for the Smart Microgrid system.
// ===========================================================================================================
namespace SmartMicrogrid.API.Data
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "SmartMicrogridDB";
    }
}
