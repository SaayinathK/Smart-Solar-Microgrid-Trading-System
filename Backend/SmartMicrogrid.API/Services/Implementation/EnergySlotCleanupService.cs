// ===========================================================================================================
// File: EnergySlotCleanupService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Business logic service implementing EnergySlotCleanupService operations, rules, and workflows.
// ===========================================================================================================
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartMicrogrid.API.Repositories.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class EnergySlotCleanupService : BackgroundService
    {
        private readonly ILogger<EnergySlotCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
        /// <summary>
        /// Initializes a new instance of the EnergySlotCleanupService class.
        /// </summary>

        public EnergySlotCleanupService(ILogger<EnergySlotCleanupService> logger, IServiceProvider serviceProvider)
        {
            // Initialize dependencies and state
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// Handles execution of async.
        /// </summary>

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Execute execute async operations
            _logger.LogInformation("EnergySlotCleanupService starting.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await CleanupExpiredSlotsAsync();
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred executing slot cleanup.");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown also cancels the delay when API startup fails.
            }
        }
        /// <summary>
        /// Performs cleanup expired slots async operation.
        /// </summary>

        private async Task CleanupExpiredSlotsAsync()
        {
            // Execute cleanup expired slots async operations
            using var scope = _serviceProvider.CreateScope();
            var slotRepository = scope.ServiceProvider.GetRequiredService<IEnergySlotRepository>();
            var microgridRepository = scope.ServiceProvider.GetRequiredService<IMicrogridRepository>();

            var now = DateTime.UtcNow;
            var allSlots = await slotRepository.GetAllAsync(null, "Available", null, null, null);
            var expiredSlots = allSlots.Where(s => s.EndTime <= now && s.AvailableAmount > 0).ToList();

            if (!expiredSlots.Any()) return;

            _logger.LogInformation($"Found {expiredSlots.Count} expired energy slots. Processing...");

            foreach (var slot in expiredSlots)
            {
                // Update slot status to Expired
                await slotRepository.UpdateStatusAsync(slot.Id!, "Expired");

                // Return reserved capacity to the microgrid
                var microgrid = await microgridRepository.GetByIdAsync(slot.MicrogridNodeId);
                if (microgrid != null)
                {
                    var newAvailable = microgrid.AvailableCapacity + slot.AvailableAmount;
                    if (newAvailable > microgrid.Capacity) newAvailable = microgrid.Capacity;
                    
                    var newReserved = microgrid.ReservedCapacity - slot.AvailableAmount;
                    if (newReserved < 0) newReserved = 0;

                    await microgridRepository.UpdateCapacityAsync(microgrid.Id!, microgrid.Capacity, newAvailable, newReserved, microgrid.UsedCapacity);
                }
            }

            _logger.LogInformation("Finished processing expired slots.");
        }
    }
}
