// ===========================================================================================================
// File: PasswordHelper.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Helper utilities providing Password functionalities.
// ===========================================================================================================
using BCrypt.Net;

namespace SmartMicrogrid.API.Helpers
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Performs hash password operation.
        /// </summary>
        public static string HashPassword(string password)
        {
            // Execute hash password operations
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        /// <summary>
        /// Verifies and validates password criteria.
        /// </summary>

        public static bool VerifyPassword(string password, string hash)
        {
            // Execute verify password operations
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
