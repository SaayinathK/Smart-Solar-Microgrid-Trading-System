// ===========================================================================================================
// File: TransactionConflictException.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Defines TransactionConflictException components for the Smart Microgrid system.
// ===========================================================================================================
namespace SmartMicrogrid.API.Services
{
    public sealed class TransactionConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the TransactionConflictException class.
        /// </summary>
        public TransactionConflictException(string message) : base(message)
        {
        }
    }
}
