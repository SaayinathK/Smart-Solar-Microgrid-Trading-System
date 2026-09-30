using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using SmartMicrogrid.API.Models.M4;
using SmartMicrogrid.API.Repositories.Interfaces.M4;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation.M4
{
    public class AuditService : IAuditService
    {
        private readonly ISystemActivityRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ISystemActivityRepository repository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public Task RecordAsync(
            string action,
            string module,
            string description,
            string? userId = null,
            string? userName = null,
            string? role = null,
            string? entityType = null,
            string? entityId = null,
            string? ipAddress = null,
            string status = AuditStatus.Success)
        {
            var principal = _httpContextAccessor.HttpContext?.User;

            var activity = new SystemActivity
            {
                UserId = userId ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? principal?.FindFirstValue("sub"),
                UserName = userName ?? ResolveUserName(principal),
                Role = role ?? principal?.FindFirstValue(ClaimTypes.Role)
                          ?? principal?.FindFirstValue("role"),
                Action = action,
                Module = module,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress ?? ResolveIpAddress(),
                Timestamp = DateTime.UtcNow,
                Status = status
            };

            return PersistAsync(activity);
        }

        private async Task PersistAsync(SystemActivity activity)
        {
            try
            {
                await _repository.RecordAsync(activity);
            }
            catch (Exception ex)
            {
                // Auditing must never break the business operation it is recording.
                _logger.LogError(ex, "Failed to persist audit record for action {Action}", activity.Action);
            }
        }

        private static string ResolveUserName(ClaimsPrincipal? principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return "Anonymous";

            var given = principal.FindFirstValue(ClaimTypes.GivenName)
                        ?? principal.FindFirstValue("given_name");
            var family = principal.FindFirstValue(ClaimTypes.Surname)
                         ?? principal.FindFirstValue(JwtRegisteredClaimNames.FamilyName);
            var email = principal.FindFirstValue(ClaimTypes.Email)
                        ?? principal.FindFirstValue("email");

            var fullName = string.Join(' ', new[] { given, family }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            return string.IsNullOrWhiteSpace(email) ? "Unknown" : email;
        }

        private string? ResolveIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return null;

            // Reverse proxy / IIS may populate X-Forwarded-For.
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
