using SmartMicrogrid.API.DTOs.Users;

namespace SmartMicrogrid.API.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public UserResponseDto User { get; set; } = null!;
    }
}
