// ===========================================================================================================
// File: SystemHealthService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M4 – Platform Administration & System Operations
// Section Owned: M4 – Platform Administration & System Operations
// Author: S. Sriramana (IT23136724)
// Description: Business logic service implementing SystemHealthService operations, rules, and workflows.
// ===========================================================================================================
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using SmartMicrogrid.API.Data;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation.M4
{
    /// <summary>
    /// M4 system health. The database status is the result of a real ping against
    /// MongoDB, never a hard-coded "Healthy".
    /// </summary>
    public class SystemHealthService : ISystemHealthService
    {
        private const string PingCommandName = "ping";

        private readonly MongoDbContext _context;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SystemHealthService> _logger;
        private readonly Stopwatch _uptime = Stopwatch.StartNew();
        /// <summary>
        /// Initializes a new instance of the SystemHealthService class.
        /// </summary>

        public SystemHealthService(
            MongoDbContext context,
            IHostEnvironment environment,
            IConfiguration configuration,
            ILogger<SystemHealthService> logger)
        {
            // Initialize dependencies and state
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }
        /// <summary>
        /// Retrieves health async details.
        /// </summary>

        public async Task<SystemHealthDto> GetHealthAsync()
        {
            // Execute get health async operations
            var health = new SystemHealthDto
            {
                Api = "Healthy",
                Authentication = "Healthy",
                Server = "Online",
                Environment = _environment.EnvironmentName,
                Version = ResolveVersion(),
                ServerTime = DateTime.UtcNow,
                Uptime = _uptime.Elapsed,
                ProcessMemoryBytes = Environment.WorkingSet,
                JwtIssuer = _configuration["Jwt:Issuer"] ?? "SmartMicrogridAPI"
            };

            await CheckDatabaseAsync(health);
            return health;
        }
        /// <summary>
        /// Verifies and validates database async criteria.
        /// </summary>

        private async Task CheckDatabaseAsync(SystemHealthDto health)
        {
            // Execute check database async operations
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // A real round-trip to the server, not a cached connection check.
                var result = await _context.Database.RunCommandAsync<BsonDocument>(
                    new BsonDocument(PingCommandName, 1));

                stopwatch.Stop();
                health.DatabaseLatencyMs = stopwatch.Elapsed.TotalMilliseconds;

                if (result.TryGetValue("ok", out var ok) && ok.ToDouble() == 1d)
                {
                    health.Database = "Healthy";
                    health.DatabaseDetail = $"Responded in {health.DatabaseLatencyMs:0} ms.";
                }
                else
                {
                    health.Database = "Degraded";
                    health.DatabaseDetail = "MongoDB responded but did not report ok.";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                health.Database = "Unhealthy";
                health.DatabaseLatencyMs = stopwatch.Elapsed.TotalMilliseconds;

                // Log the detail, but never return the connection string to the client.
                _logger.LogError(ex, "MongoDB health check failed.");
                health.DatabaseDetail = "MongoDB is not responding.";
            }
        }
        /// <summary>
        /// Performs resolve version operation.
        /// </summary>

        private static string ResolveVersion()
        {
            // Execute resolve version operations
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version == null
                ? "1.0.0"
                : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
