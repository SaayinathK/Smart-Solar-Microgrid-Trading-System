// ===========================================================================================================
// File: JwtHelper.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Helper utilities providing Jwt functionalities.
// ===========================================================================================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;
        /// <summary>
        /// Initializes a new instance of the JwtHelper class.
        /// </summary>

        public JwtHelper(IConfiguration configuration)
        {
            // Initialize dependencies and state
            _configuration = configuration;
        }
        /// <summary>
        /// Performs generate jwt token operation.
        /// </summary>

        public (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
        {
            // Execute generate jwt token operations
            var secretKey = _configuration["Jwt:SecretKey"] ?? "SmartMicrogrid_Default_Super_Secret_Key_2026_EAD!";
            var issuer = _configuration["Jwt:Issuer"] ?? "SmartMicrogridAPI";
            var audience = _configuration["Jwt:Audience"] ?? "SmartMicrogridClients";
            var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryInMinutes"], out var minutes) ? minutes : 480;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
