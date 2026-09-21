using BCrypt.Net;

namespace SmartMicrogrid.API.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
